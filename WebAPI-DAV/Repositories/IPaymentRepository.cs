using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public interface IPaymentRepository
    {
        (bool Success, string Message, PaymentUrlResponseDTO? Result) CreatePaymentUrl(int bookingId, string userId, string returnUrl, string ipAddress);
        PaymentResultDTO HandleVnPayResult(IDictionary<string, string> data);
        List<PaymentListDTO> GetAll();

        // Chuyển khoản ngân hàng (MB Bank) + SePay báo biến động số dư
        (bool Success, string Message, BankTransferInfoDTO? Result) GetBankTransferInfo(int bookingId, string userId);
        bool IsValidSePayKey(string? authorizationHeader);
        (bool Confirmed, string Message) HandleSePayWebhook(SePayWebhookDTO dto);
    }
}
