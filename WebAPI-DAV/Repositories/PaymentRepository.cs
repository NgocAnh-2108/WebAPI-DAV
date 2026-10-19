using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAPI_DAV.Data;
using WebAPI_DAV.Helpers;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    // Kho thanh toán: tạo link VNPay để khách trả cọc thật, xử lý kết quả VNPay trả về
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly VnPayOptions _vnPay;
        private readonly SePayOptions _sePay;

        public PaymentRepository(AppDbContext dbContext, IOptions<VnPayOptions> vnPay, IOptions<SePayOptions> sePay)
        {
            _dbContext = dbContext;
            _vnPay = vnPay.Value;
            _sePay = sePay.Value;
        }

        // ================= CHUYỂN KHOẢN NGÂN HÀNG (MB Bank + SePay) =================

        // Thông tin để web hiện QR: số TK, số tiền cọc, nội dung = mã booking
        public (bool Success, string Message, BankTransferInfoDTO? Result) GetBankTransferInfo(int bookingId, string userId)
        {
            if (!_sePay.IsConfigured)
                return (false, "Khách sạn chưa cấu hình tài khoản nhận chuyển khoản (mục SePay trong appsettings.json)", null);

            var booking = _dbContext.Bookings.Include(b => b.Customer).FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng", null);
            if (booking.Customer.UserId != userId) return (false, "Bạn không có quyền xem đặt phòng này", null);

            var content = SePayHelper.PaymentContent(booking.BookingCode);
            return (true, "OK", new BankTransferInfoDTO
            {
                BankCode = _sePay.BankCode,
                AccountNumber = _sePay.AccountNumber,
                AccountName = _sePay.AccountName,
                Amount = booking.DepositAmount,
                Content = content,
                QrImageUrl = SePayHelper.QrImageUrl(_sePay, booking.DepositAmount, content)
            });
        }

        public bool IsValidSePayKey(string? authorizationHeader)
        {
            // Chưa đặt ApiKey (đang thử nghiệm với "Không xác thực") -> cho qua; chạy thật nên luôn đặt ApiKey
            if (string.IsNullOrWhiteSpace(_sePay.ApiKey)) return true;
            return authorizationHeader == "Apikey " + _sePay.ApiKey;
        }

        // SePay báo có tiền vào tài khoản -> tìm mã booking trong nội dung chuyển, đủ tiền cọc thì xác nhận booking
        public (bool Confirmed, string Message) HandleSePayWebhook(SePayWebhookDTO dto)
        {
            if (!string.Equals(dto.TransferType, "in", StringComparison.OrdinalIgnoreCase))
                return (false, "Bỏ qua giao dịch tiền ra");

            // SePay có thể gửi lại cùng 1 giao dịch (tự thử lại) -> đã xử lý rồi thì bỏ qua
            var gatewayNo = "SEPAY-" + dto.Id;
            if (dto.Id > 0 && _dbContext.Payments.Any(p => p.GatewayTransactionNo == gatewayNo))
                return (false, "Giao dịch đã được xử lý trước đó");

            var code = SePayHelper.FindBookingCode(dto.Code, dto.Content, dto.Description);
            if (code == null) return (false, "Nội dung chuyển khoản không có mã booking");

            var booking = _dbContext.Bookings.FirstOrDefault(b => b.BookingCode == code);
            if (booking == null) return (false, $"Không tìm thấy booking {code}");

            var payment = new Payment
            {
                TransactionCode = "BANK" + (string.IsNullOrEmpty(dto.ReferenceCode) ? dto.Id.ToString() : dto.ReferenceCode),
                BookingId = booking.Id,
                Amount = dto.TransferAmount,
                Method = "Bank",
                GatewayTransactionNo = gatewayNo,
                BankCode = dto.Gateway,
                CreatedDate = DateTime.Now,
                CompletedDate = DateTime.Now,
                Status = "Success"
            };
            _dbContext.Payments.Add(payment);

            string message;
            var confirmed = false;
            if (booking.Status == "Pending" && !booking.IsDepositPaid)
            {
                if (dto.TransferAmount >= booking.DepositAmount)
                {
                    booking.IsDepositPaid = true;
                    booking.Status = "Confirmed";
                    confirmed = true;
                    message = $"Đã nhận {dto.TransferAmount:N0}đ, booking {code} được xác nhận";
                }
                else
                {
                    // Chuyển thiếu: ghi nhận tiền nhưng chưa xác nhận, lễ tân liên hệ khách
                    payment.Status = "Underpaid";
                    message = $"Booking {code} chuyển thiếu ({dto.TransferAmount:N0}/{booking.DepositAmount:N0}đ), chưa xác nhận";
                }
            }
            else
            {
                // Booking đã hủy / đã cọc trước đó -> tiền này phải hoàn lại cho khách
                booking.RefundAmount += dto.TransferAmount;
                booking.RefundStatus = "Pending";
                message = $"Booking {code} không còn chờ cọc, ghi nhận {dto.TransferAmount:N0}đ cần hoàn cho khách";
            }

            _dbContext.SaveChanges();
            return (confirmed, message);
        }

        // B1: khách bấm "Thanh toán cọc qua VNPay" -> tạo giao dịch Pending + link VNPay đã ký
        public (bool Success, string Message, PaymentUrlResponseDTO? Result) CreatePaymentUrl(
            int bookingId, string userId, string returnUrl, string ipAddress)
        {
            if (!_vnPay.IsConfigured)
                return (false, "Khách sạn chưa cấu hình VNPay (TmnCode/HashSecret trong appsettings.json). Vui lòng chuyển khoản MoMo hoặc đặt cọc tại quầy.", null);

            var booking = _dbContext.Bookings.Include(b => b.Customer).FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
                return (false, "Không tìm thấy đặt phòng", null);
            if (booking.Customer.UserId != userId)
                return (false, "Bạn không có quyền thanh toán cho đặt phòng này", null);
            if (booking.IsDepositPaid)
                return (false, "Đặt phòng này đã được thanh toán cọc", null);
            if (booking.Status != "Pending")
                return (false, $"Booking đang ở trạng thái '{booking.Status}', không thể thanh toán", null);

            // Mã giao dịch gửi sang VNPay (vnp_TxnRef) — chỉ dùng chữ và số
            var transactionCode = "TXN" + DateTime.Now.ToString("yyMMddHHmmss") + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            var payment = new Payment
            {
                TransactionCode = transactionCode,
                BookingId = booking.Id,
                Amount = booking.DepositAmount,
                Status = "Pending",
                Method = "VNPay",
                CreatedDate = DateTime.Now
            };
            _dbContext.Payments.Add(payment);
            _dbContext.SaveChanges();

            var vnNow = VietnamTime.Now;
            var parameters = new Dictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _vnPay.TmnCode,
                ["vnp_Amount"] = ((long)(payment.Amount * 100)).ToString(),   // VNPay tính theo đơn vị x100
                ["vnp_CreateDate"] = vnNow.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = vnNow.AddMinutes(_vnPay.ExpireMinutes).ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = $"Dat coc dat phong {booking.BookingCode}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = transactionCode
            };

            var result = new PaymentUrlResponseDTO
            {
                PaymentUrl = VnPayHelper.CreatePaymentUrl(_vnPay, parameters),
                TransactionCode = transactionCode
            };
            return (true, "Tạo link thanh toán VNPay thành công", result);
        }

        // B2: VNPay trả kết quả (qua trình duyệt của khách hoặc gọi IPN)
        // Kiểm tra chữ ký + số tiền rồi mới xác nhận booking -> không ai giả mạo được kết quả
        public PaymentResultDTO HandleVnPayResult(IDictionary<string, string> data)
        {
            var result = new PaymentResultDTO();
            data.TryGetValue("vnp_TxnRef", out var txnRef);
            data.TryGetValue("vnp_ResponseCode", out var responseCode);
            data.TryGetValue("vnp_TransactionStatus", out var transactionStatus);
            data.TryGetValue("vnp_Amount", out var amountRaw);
            result.TransactionCode = txnRef;
            result.ResponseCode = responseCode;

            if (!_vnPay.IsConfigured || !VnPayHelper.ValidateSignature(_vnPay, data))
            {
                result.RspCode = "97";
                result.Message = "Chữ ký VNPay không hợp lệ — giao dịch bị từ chối";
                return result;
            }

            var payment = _dbContext.Payments.Include(p => p.Booking).FirstOrDefault(p => p.TransactionCode == txnRef);
            if (payment == null)
            {
                result.RspCode = "01";
                result.Message = "Không tìm thấy giao dịch";
                return result;
            }
            result.BookingId = payment.BookingId;
            result.BookingCode = payment.Booking.BookingCode;
            result.Amount = payment.Amount;

            if (!long.TryParse(amountRaw, out var amountX100) || amountX100 != (long)(payment.Amount * 100))
            {
                result.RspCode = "04";
                result.Message = "Số tiền thanh toán không khớp";
                return result;
            }

            // Giao dịch đã xử lý trước đó (khách F5 trang kết quả, hoặc IPN đã gọi trước) -> trả lại kết quả cũ
            if (payment.Status != "Pending")
            {
                result.RspCode = "02";
                result.Success = payment.Status == "Success";
                result.Message = result.Success ? "Thanh toán cọc thành công, đặt phòng đã được xác nhận" : "Giao dịch không thành công";
                return result;
            }

            var isSuccess = responseCode == "00" && (transactionStatus == null || transactionStatus == "00");
            payment.Status = isSuccess ? "Success" : "Failed";
            payment.CompletedDate = DateTime.Now;
            payment.GatewayTransactionNo = data.TryGetValue("vnp_TransactionNo", out var tno) ? tno : null;
            payment.BankCode = data.TryGetValue("vnp_BankCode", out var bank) ? bank : null;

            if (isSuccess)
            {
                var booking = payment.Booking;
                if (booking.Status == "Pending" && !booking.IsDepositPaid)
                {
                    booking.IsDepositPaid = true;
                    booking.Status = "Confirmed";
                    result.Message = "Thanh toán cọc thành công, đặt phòng đã được xác nhận";
                }
                else
                {
                    // Booking đã hủy / đã cọc bằng cách khác trong lúc khách đang thanh toán -> ghi nhận để hoàn tiền
                    booking.RefundAmount += payment.Amount;
                    booking.RefundStatus = "Pending";
                    result.Message = "Đã nhận tiền nhưng booking không còn chờ cọc — khách sạn sẽ hoàn lại khoản này";
                }
            }
            else
            {
                result.Message = VnPayHelper.ResponseMessage(responseCode);
            }

            _dbContext.SaveChanges();
            result.Success = isSuccess;
            result.RspCode = "00";
            return result;
        }

        // Danh sách tất cả giao dịch thanh toán (cho Manager xem trang Tài chính > Thanh toán)
        public List<PaymentListDTO> GetAll()
        {
            return _dbContext.Payments
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => new PaymentListDTO
                {
                    Id = p.Id,
                    TransactionCode = p.TransactionCode,
                    BookingId = p.BookingId,
                    BookingCode = p.Booking.BookingCode,
                    CustomerName = p.Booking.Customer.FullName,
                    Amount = p.Amount,
                    Status = p.Status,
                    CreatedDate = p.CreatedDate,
                    CompletedDate = p.CompletedDate,
                    Method = p.Method,
                    GatewayTransactionNo = p.GatewayTransactionNo,
                    BankCode = p.BankCode
                }).ToList();
        }
    }
}
