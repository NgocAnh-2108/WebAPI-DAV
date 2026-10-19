namespace Hotel_Web.Models.DTO
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
        public List<string> RoomNumbers { get; set; } = new();
        public List<BookingRoomInfoDTO> Rooms { get; set; } = new();

        // Hủy phòng / hoàn cọc
        public DateTime? CancelledDate { get; set; }
        public string? CancelReason { get; set; }
        public decimal CancellationFee { get; set; }
        public decimal RefundAmount { get; set; }
        public string? RefundStatus { get; set; }   // null / Pending / Done
        public string? RefundBankCode { get; set; }
        public string? RefundAccountNumber { get; set; }
        public string? RefundAccountName { get; set; }
        public DateTime? RefundedDate { get; set; }
        public string? RefundNote { get; set; }

        // Chuyển phòng
        public decimal RoomChangeDiscount { get; set; }

        public int Nights => Math.Max(0, (CheckOutDate.Date - CheckInDate.Date).Days);
        public bool IsClosed => Status is "CheckedOut" or "Cancelled" or "NoShow";
    }

    public class BookingRoomInfoDTO
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public decimal BasePrice { get; set; }
    }

    public class CustomerBookingRequestDTO
    {
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? Note { get; set; }
        public List<int> RoomIds { get; set; } = new();
        public List<ServiceItemDTO> Services { get; set; } = new();
    }

    public class ServiceItemDTO
    {
        public int ServiceId { get; set; }
        public int Quantity { get; set; }
    }

    // Kết quả API tính tiền khi hủy (chính sách hủy)
    public class CancellationQuoteDTO
    {
        public bool CanCancel { get; set; }
        public decimal DepositPaid { get; set; }
        public int FeePercent { get; set; }
        public decimal Fee { get; set; }
        public decimal Refund { get; set; }
        public string Rule { get; set; } = "";
        public DateTime? FreeCancelUntil { get; set; }
    }

    public class RefundAccountDTO
    {
        public string BankCode { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";
    }

    public class MarkRefundedDTO
    {
        public string Method { get; set; } = "Bank";
        public string? Note { get; set; }
    }

    public class ChangeBookingTimeDTO
    {
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }

    public class StaffCancelDTO
    {
        public bool ByHotel { get; set; }
        public string? Reason { get; set; }
    }

    // ===== Chuyển phòng =====
    public class TransferOptionDTO
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = "";
        public int Floor { get; set; }
        public string RoomTypeName { get; set; } = "";
        public decimal BasePrice { get; set; }
        public string Tier { get; set; } = "";
        public decimal EstimatedDiscount { get; set; }
    }

    public class TransferOptionsResultDTO
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public int FromRoomId { get; set; }
        public string FromRoomNumber { get; set; } = "";
        public string FromRoomTypeName { get; set; } = "";
        public decimal FromPrice { get; set; }
        public int RemainingNights { get; set; }
        public string Policy { get; set; } = "";
        public string PolicyMessage { get; set; } = "";
        public List<BookingRoomInfoDTO> CurrentRooms { get; set; } = new();
        public List<TransferOptionDTO> Options { get; set; } = new();
    }

    public class TransferRoomRequestDTO
    {
        public int FromRoomId { get; set; }
        public int ToRoomId { get; set; }
        public string? Reason { get; set; }
        public bool MarkOldRoomMaintenance { get; set; } = true;
    }

    public class RoomTransferDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string FromRoomNumber { get; set; } = "";
        public string ToRoomNumber { get; set; } = "";
        public decimal OldPricePerNight { get; set; }
        public decimal NewPricePerNight { get; set; }
        public int RemainingNights { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
