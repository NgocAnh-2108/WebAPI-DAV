using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.Domain
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public DateTime CheckInDate { get; set; }        // gồm cả GIỜ nhận phòng (mặc định 14:00)
        public DateTime CheckOutDate { get; set; }       // gồm cả GIỜ trả phòng (mặc định 12:00)
        public string Status { get; set; }              // Pending, Confirmed, CheckedIn, CheckedOut, Cancelled, NoShow
        public decimal TotalPrice { get; set; }         // tiền phòng (đã trừ giảm trừ chuyển phòng nếu có)
        public decimal DepositAmount { get; set; }      // tiền cọc (30% TotalPrice)
        public bool IsDepositPaid { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Note { get; set; }

        // ===== Hủy phòng / No-show (theo chính sách hủy) =====
        public DateTime? CancelledDate { get; set; }
        public string? CancelReason { get; set; }       // lý do + quy định đã áp dụng
        public decimal CancellationFee { get; set; }    // phần tiền cọc khách sạn giữ lại
        public decimal RefundAmount { get; set; }       // phần tiền cọc phải hoàn cho khách
        public string? RefundStatus { get; set; }       // null = không cần hoàn, Pending = chờ hoàn, Done = đã hoàn
        public string? RefundBankCode { get; set; }     // ngân hàng khách nhận tiền hoàn (mã VietQR: MB, VCB, TCB...)
        public string? RefundAccountNumber { get; set; }
        public string? RefundAccountName { get; set; }
        public DateTime? RefundedDate { get; set; }     // ngày khách sạn đã hoàn tiền
        public string? RefundNote { get; set; }         // hình thức hoàn + ghi chú (VD: chuyển khoản MB, mã GD...)

        // ===== Chuyển phòng =====
        public decimal RoomChangeDiscount { get; set; } // tiền giảm trừ khi phải chuyển xuống phòng hạng thấp hơn

        // Navigation - Booking thuộc 1 Customer
        public int CustomerID { get; set; }
        public Customer Customer { get; set; }

        // Navigation - 1 Booking có nhiều BookingRoom (đặt nhiều phòng)
        public List<BookingRoom> BookingRooms { get; set; }

        // Navigation - 1 Booking có nhiều dịch vụ khách sử dụng
        public List<BookingService> BookingServices { get; set; }

        // Navigation - lịch sử chuyển phòng
        public List<RoomTransfer> RoomTransfers { get; set; }
    }
}
