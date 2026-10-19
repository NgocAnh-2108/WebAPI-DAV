using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.DTO
{
    public class BookingWithDetailDTO
    {
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public bool IsDepositPaid { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Note { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public List<string> RoomNumbers { get; set; }
        public List<BookingRoomInfoDTO> Rooms { get; set; } = new();

        // Hủy phòng / hoàn cọc
        public DateTime? CancelledDate { get; set; }
        public string? CancelReason { get; set; }
        public decimal CancellationFee { get; set; }
        public decimal RefundAmount { get; set; }
        public string? RefundStatus { get; set; }
        public string? RefundBankCode { get; set; }
        public string? RefundAccountNumber { get; set; }
        public string? RefundAccountName { get; set; }
        public DateTime? RefundedDate { get; set; }
        public string? RefundNote { get; set; }

        // Chuyển phòng
        public decimal RoomChangeDiscount { get; set; }
    }

    public class BookingRoomInfoDTO
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class AddBookingRequestDTO // dùng cho Receptionist/Manager (có CustomerID)
    {
        [Required] public DateTime CheckInDate { get; set; }
        [Required] public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Note { get; set; }
        public int CustomerID { get; set; }
        [Required][MinLength(1)] public List<int> RoomIds { get; set; }
    }

    public class CustomerBookingRequestDTO // dùng cho Customer tự đặt, không cần CustomerID
    {
        [Required] public DateTime CheckInDate { get; set; }
        [Required] public DateTime CheckOutDate { get; set; }
        public string? Note { get; set; }
        [Required][MinLength(1)] public List<int> RoomIds { get; set; }

        // Dịch vụ khách chọn kèm khi đặt phòng (tùy chọn) — thanh toán khi trả phòng
        public List<ServiceItemDTO>? Services { get; set; }
    }

    public class ServiceItemDTO
    {
        public int ServiceId { get; set; }
        [Range(1, 100)] public int Quantity { get; set; } = 1;
    }

    // Lễ tân đổi giờ nhận / giờ trả phòng cho khách (nhận sớm, trả muộn, gia hạn...)
    public class ChangeBookingTimeDTO
    {
        [Required] public DateTime CheckInDate { get; set; }
        [Required] public DateTime CheckOutDate { get; set; }
    }

    // Nhân viên hủy booking
    public class StaffCancelRequestDTO
    {
        public bool ByHotel { get; set; }     // true = khách sạn hủy (hoàn 100% cọc), false = hủy theo yêu cầu khách (áp chính sách)
        public string? Reason { get; set; }
    }

    // Khách khai báo tài khoản nhận tiền hoàn cọc
    public class RefundAccountDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn ngân hàng")]
        [RegularExpression(@"^[A-Za-z]{2,5}$", ErrorMessage = "Mã ngân hàng không hợp lệ")]
        public string BankCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản")]
        [RegularExpression(@"^\d{6,19}$", ErrorMessage = "Số tài khoản chỉ gồm chữ số, từ 6 đến 19 số")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản")]
        [RegularExpression(@"^[\p{L} ]{2,50}$", ErrorMessage = "Tên chủ tài khoản chỉ gồm chữ cái và khoảng trắng")]
        public string AccountName { get; set; }
    }

    // Nhân viên xác nhận đã hoàn tiền
    public class MarkRefundedDTO
    {
        public string Method { get; set; } = "Bank";   // Bank = chuyển khoản, Cash = tiền mặt tại quầy
        public string? Note { get; set; }
    }

    // ===== Chuyển phòng =====
    public class TransferOptionDTO
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public int Floor { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
        public string Tier { get; set; }                 // Same (cùng hạng), Lower (hạng thấp hơn), Higher (nâng hạng miễn phí)
        public decimal EstimatedDiscount { get; set; }   // tiền sẽ được trừ vào hóa đơn
    }

    public class TransferOptionsResultDTO
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }
        public int FromRoomId { get; set; }
        public string FromRoomNumber { get; set; }
        public string FromRoomTypeName { get; set; }
        public decimal FromPrice { get; set; }
        public int RemainingNights { get; set; }
        public string Policy { get; set; }               // Same / Lower / Higher / None
        public string PolicyMessage { get; set; }
        public List<BookingRoomInfoDTO> CurrentRooms { get; set; } = new();
        public List<TransferOptionDTO> Options { get; set; } = new();
    }

    public class TransferRoomRequestDTO
    {
        [Required] public int FromRoomId { get; set; }
        [Required] public int ToRoomId { get; set; }
        public string? Reason { get; set; }
        public bool MarkOldRoomMaintenance { get; set; } = true; // phòng cũ có sự cố -> chuyển sang bảo trì
    }

    public class RoomTransferDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string FromRoomNumber { get; set; }
        public string ToRoomNumber { get; set; }
        public decimal OldPricePerNight { get; set; }
        public decimal NewPricePerNight { get; set; }
        public int RemainingNights { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
