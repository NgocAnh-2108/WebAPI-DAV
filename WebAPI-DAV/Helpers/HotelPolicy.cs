using WebAPI_DAV.Models.Domain;

namespace WebAPI_DAV.Helpers
{
    // Cấu hình giờ nhận/trả phòng — đọc từ appsettings.json, mục "HotelPolicy"
    public class HotelPolicyOptions
    {
        public string CheckInTime { get; set; } = "14:00";   // giờ nhận phòng mặc định
        public string CheckOutTime { get; set; } = "12:00";  // giờ trả phòng mặc định

        public TimeSpan CheckInSpan => TimeSpan.TryParse(CheckInTime, out var t) ? t : new TimeSpan(14, 0, 0);
        public TimeSpan CheckOutSpan => TimeSpan.TryParse(CheckOutTime, out var t) ? t : new TimeSpan(12, 0, 0);

        // Khách chỉ chọn NGÀY (giờ = 00:00) -> tự gắn giờ mặc định của khách sạn
        public DateTime ApplyCheckInTime(DateTime d) => d.TimeOfDay == TimeSpan.Zero ? d.Date + CheckInSpan : d;
        public DateTime ApplyCheckOutTime(DateTime d) => d.TimeOfDay == TimeSpan.Zero ? d.Date + CheckOutSpan : d;
    }

    // Kết quả tính tiền khi hủy phòng
    public class CancellationQuote
    {
        public bool CanCancel { get; set; }
        public decimal DepositPaid { get; set; }     // tiền cọc khách đã trả
        public int FeePercent { get; set; }          // % tiền cọc bị giữ lại (0 / 50 / 100)
        public decimal Fee { get; set; }             // tiền khách sạn giữ lại
        public decimal Refund { get; set; }          // tiền hoàn lại cho khách
        public string Rule { get; set; } = "";       // quy định được áp dụng (hiển thị cho khách)
        public DateTime? FreeCancelUntil { get; set; } // hạn chót hủy miễn phí
    }

    // ===== CHÍNH SÁCH HỦY PHÒNG =====
    // Số đêm  | Hủy miễn phí trước | Hủy trễ hạn  | Hủy trong 24h trước check-in
    // 1 đêm   | 24 giờ             | —            | giữ 100% cọc
    // 2-3 đêm | 3 ngày             | giữ 50% cọc  | giữ 100% cọc
    // 4-6 đêm | 5 ngày             | giữ 50% cọc  | giữ 100% cọc
    // >= 7    | 7 ngày             | giữ 50% cọc  | giữ 100% cọc
    // Không đến (No-show): giữ 100% cọc. Khách sạn hủy: hoàn 100% cọc.
    // Đặt sát ngày (dưới 24 giờ trước check-in): hủy miễn phí trong 1 giờ sau khi đặt, quá 1 giờ giữ 100% cọc.
    public static class CancellationPolicy
    {
        public static int FreeCancelHours(int nights) =>
            nights <= 1 ? 24 : nights <= 3 ? 72 : nights <= 6 ? 120 : 168;

        public static string FreeCancelLabel(int nights) =>
            nights <= 1 ? "24 giờ" : nights <= 3 ? "3 ngày" : nights <= 6 ? "5 ngày" : "7 ngày";

        public static int Nights(Booking b) => Math.Max(1, (b.CheckOutDate.Date - b.CheckInDate.Date).Days);

        public static CancellationQuote Quote(Booking b, DateTime now, bool byHotel = false)
        {
            var paid = b.IsDepositPaid ? b.DepositAmount : 0;
            var q = new CancellationQuote { DepositPaid = paid };

            if (b.Status != "Pending" && b.Status != "Confirmed")
            {
                q.Rule = $"Booking đang ở trạng thái '{b.Status}', không thể hủy";
                return q;
            }
            q.CanCancel = true;

            if (byHotel) return Set(q, 0, "Khách sạn hủy phòng: hoàn 100% tiền cọc");
            if (paid == 0) return Set(q, 0, "Chưa thanh toán cọc: hủy miễn phí");

            var nights = Nights(b);

            // Đặt sát ngày: đặt khi còn dưới 24 giờ là tới giờ nhận phòng (chưa check-in thì vẫn áp dụng 1 giờ miễn phí)
            if ((b.CheckInDate - b.CreatedDate).TotalHours < 24)
            {
                q.FreeCancelUntil = b.CreatedDate.AddHours(1);
                return now <= q.FreeCancelUntil
                    ? Set(q, 0, "Đặt sát ngày: được hủy miễn phí trong 1 giờ sau khi đặt")
                    : Set(q, 100, "Đặt sát ngày và đã quá 1 giờ sau khi đặt: giữ 100% tiền cọc");
            }

            if (now >= b.CheckInDate)
                return Set(q, 100, "Đã quá giờ nhận phòng (không đến nhận phòng): giữ 100% tiền cọc");

            q.FreeCancelUntil = b.CheckInDate.AddHours(-FreeCancelHours(nights));
            var hoursLeft = (b.CheckInDate - now).TotalHours;

            if (hoursLeft >= FreeCancelHours(nights))
                return Set(q, 0, $"Hủy trước {FreeCancelLabel(nights)} so với giờ nhận phòng: hoàn 100% tiền cọc");
            if (hoursLeft >= 24)
                return Set(q, 50, $"Hủy trễ hạn (sau mốc {FreeCancelLabel(nights)} trước nhận phòng): giữ 50% tiền cọc");
            return Set(q, 100, "Hủy trong vòng 24 giờ trước giờ nhận phòng: giữ 100% tiền cọc");
        }

        private static CancellationQuote Set(CancellationQuote q, int feePercent, string rule)
        {
            q.FeePercent = feePercent;
            q.Fee = Math.Round(q.DepositPaid * feePercent / 100m, 0);
            q.Refund = q.DepositPaid - q.Fee;
            q.Rule = rule;
            return q;
        }
    }

    // Giờ Việt Nam (GMT+7) — VNPay yêu cầu thời gian theo múi giờ này
    public static class VietnamTime
    {
        public static DateTime Now
        {
            get
            {
                foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
                {
                    try { return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, id); }
                    catch { }
                }
                return DateTime.UtcNow.AddHours(7);
            }
        }
    }
}
