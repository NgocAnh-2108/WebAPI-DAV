using Hotel_Web.Models.DTO;

namespace Hotel_Web.Helpers
{
    public static class MoneyHelper
    {
        // Doanh thu ghi nhận của 1 booking:
        // - Đã trả phòng: toàn bộ tiền phòng (đã trừ giảm trừ chuyển phòng)
        // - Hủy / Không đến: phần tiền cọc khách sạn giữ lại theo chính sách hủy
        // - Còn lại: tiền cọc nếu đã thanh toán
        public static decimal Earned(BookingWithDetailDTO b) => b.Status switch
        {
            "CheckedOut" => b.TotalPrice,
            "Cancelled" or "NoShow" => b.CancellationFee,
            _ => b.IsDepositPaid ? b.DepositAmount : 0
        };
    }
}
