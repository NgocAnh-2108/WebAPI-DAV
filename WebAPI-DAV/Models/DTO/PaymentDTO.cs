using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.DTO
{
    // Web gửi lên địa chỉ để VNPay chuyển khách về sau khi thanh toán xong
    public class CreatePaymentRequestDTO
    {
        [Required] public string ReturnUrl { get; set; }
    }

    public class PaymentUrlResponseDTO
    {
        public string PaymentUrl { get; set; }
        public string TransactionCode { get; set; }
    }

    // Kết quả xử lý dữ liệu VNPay trả về
    public class PaymentResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? TransactionCode { get; set; }
        public string? BookingCode { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string? ResponseCode { get; set; }
        // Mã trả lời cho VNPay khi gọi IPN: 00 ok, 01 không thấy đơn, 02 đã xử lý, 04 sai số tiền, 97 sai chữ ký, 99 lỗi khác
        public string RspCode { get; set; } = "99";
    }

    public class PaymentListDTO
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; }
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public string? CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Method { get; set; }
        public string? GatewayTransactionNo { get; set; }
        public string? BankCode { get; set; }
    }

    // Dữ liệu SePay gửi sang khi tài khoản ngân hàng có biến động số dư
    public class SePayWebhookDTO
    {
        public long Id { get; set; }                  // mã giao dịch phía SePay (không đổi khi gửi lại)
        public string? Gateway { get; set; }          // tên ngân hàng (MBBank...)
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? SubAccount { get; set; }
        public string? Code { get; set; }             // mã thanh toán SePay tách được từ nội dung (có thể null)
        public string? Content { get; set; }          // nội dung chuyển khoản gốc
        public string? TransferType { get; set; }     // "in" = tiền vào, "out" = tiền ra
        public string? Description { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal Accumulated { get; set; }
        public string? ReferenceCode { get; set; }
    }

    // Thông tin chuyển khoản để web hiện QR cho khách
    public class BankTransferInfoDTO
    {
        public string BankCode { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
        public string Content { get; set; }
        public string QrImageUrl { get; set; }
    }
}
