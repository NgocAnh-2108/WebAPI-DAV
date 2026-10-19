using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentRepository paymentRepository, ILogger<PaymentsController> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        // POST /api/Payments/create-deposit-payment/5 — khách bấm "Thanh toán cọc qua VNPay"
        // Trả về link VNPay, web chuyển khách sang đó để nhập thẻ / quét QR ngân hàng
        [HttpPost("create-deposit-payment/{bookingId}")]
        [Authorize(Roles = "Customer")]
        public IActionResult CreateDepositPayment(int bookingId, [FromBody] CreatePaymentRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
            var (success, message, result) = _paymentRepository.CreatePaymentUrl(bookingId, userId, dto.ReturnUrl, ip);
            if (!success) return BadRequest(new { message });
            return Ok(new { message, data = result });
        }

        // POST /api/Payments/vnpay-return — web chuyển tiếp các tham số vnp_... mà VNPay gắn lên URL trả về
        // Không cần JWT: tính hợp lệ được đảm bảo bằng chữ ký vnp_SecureHash
        [HttpPost("vnpay-return")]
        [AllowAnonymous]
        public IActionResult VnPayReturn([FromBody] Dictionary<string, string> vnpData)
        {
            var result = _paymentRepository.HandleVnPayResult(vnpData);
            _logger.LogInformation($"VNPay return {result.TransactionCode}: {result.RspCode} {result.Message}");
            if (result.RspCode == "97" || result.RspCode == "01" || result.RspCode == "04")
                return BadRequest(result);
            return Ok(result);
        }

        // GET /api/Payments/vnpay-ipn — VNPay gọi thẳng server-to-server (cần địa chỉ public khi chạy thật)
        // Khai báo URL này trong trang quản trị merchant của VNPay
        [HttpGet("vnpay-ipn")]
        [AllowAnonymous]
        public IActionResult VnPayIpn()
        {
            var data = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var result = _paymentRepository.HandleVnPayResult(data);
            _logger.LogInformation($"VNPay IPN {result.TransactionCode}: {result.RspCode} {result.Message}");
            var message = result.RspCode switch
            {
                "00" => "Confirm Success",
                "01" => "Order not found",
                "02" => "Order already confirmed",
                "04" => "Invalid amount",
                "97" => "Invalid signature",
                _ => "Unknown error"
            };
            return Ok(new { RspCode = result.RspCode, Message = message });
        }

        // GET /api/Payments/bank-transfer-info/5 — thông tin chuyển khoản + ảnh QR cho booking của khách
        [HttpGet("bank-transfer-info/{bookingId}")]
        [Authorize(Roles = "Customer")]
        public IActionResult BankTransferInfo(int bookingId)
        {
            var (success, message, result) = _paymentRepository.GetBankTransferInfo(bookingId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!success) return BadRequest(new { message });
            return Ok(result);
        }

        // POST /api/Payments/sepay-webhook — SePay gọi vào khi tài khoản MB có tiền chuyển đến
        // Không dùng JWT: SePay gửi header "Authorization: Apikey <khóa>" để chứng minh đúng là SePay
        // Phải trả về đúng {"success": true} thì SePay mới coi là đã nhận, nếu không sẽ gửi lại
        [HttpPost("sepay-webhook")]
        [AllowAnonymous]
        public IActionResult SePayWebhook([FromBody] SePayWebhookDTO dto)
        {
            if (!_paymentRepository.IsValidSePayKey(Request.Headers.Authorization.ToString()))
            {
                _logger.LogWarning("SePay webhook: sai API key");
                return Unauthorized(new { success = false, message = "Invalid API key" });
            }

            var (confirmed, message) = _paymentRepository.HandleSePayWebhook(dto);
            _logger.LogInformation($"SePay webhook #{dto.Id} {dto.TransferAmount:N0}đ '{dto.Content}': {message}");
            return Ok(new { success = true });
        }

        // GET /api/Payments/get-all-payments — Manager xem lịch sử giao dịch
        [HttpGet("get-all-payments")]
        [Authorize(Roles = "Manager")]
        public IActionResult GetAll()
        {
            return Ok(_paymentRepository.GetAll());
        }
    }
}
