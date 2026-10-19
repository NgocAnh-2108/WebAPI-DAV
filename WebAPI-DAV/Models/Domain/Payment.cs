namespace WebAPI_DAV.Models.Domain
{
    public class Payment
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; }    // mã giao dịch của khách sạn (gửi sang VNPay là vnp_TxnRef)
        public int BookingId { get; set; }
        public Booking Booking { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }     // Pending, Success, Failed
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string? Method { get; set; }              // VNPay, Cash (tiền mặt / MoMo lễ tân xác nhận)
        public string? GatewayTransactionNo { get; set; } // mã giao dịch phía VNPay (vnp_TransactionNo)
        public string? BankCode { get; set; }            // ngân hàng khách dùng để thanh toán
    }
}
