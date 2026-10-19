namespace Hotel_Web.Models.DTO
{
    public class CreatePaymentRequestDTO
    {
        public string ReturnUrl { get; set; } = "";
    }

    public class PaymentUrlResponseDTO
    {
        public string PaymentUrl { get; set; }
        public string TransactionCode { get; set; }
    }

    // Kết quả API trả về sau khi kiểm tra dữ liệu VNPay
    public class PaymentResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? TransactionCode { get; set; }
        public string? BookingCode { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string? ResponseCode { get; set; }
        public string RspCode { get; set; } = "";
    }

    public class BankTransferInfoDTO
    {
        public string BankCode { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Content { get; set; } = "";
        public string QrImageUrl { get; set; } = "";
    }
}
