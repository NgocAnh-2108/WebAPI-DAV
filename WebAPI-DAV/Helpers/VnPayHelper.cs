using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI_DAV.Helpers
{
    // Cấu hình VNPay — đọc từ appsettings.json, mục "VnPay"
    // Đăng ký tài khoản test (sandbox) tại https://sandbox.vnpayment.vn/devreg/ để nhận TmnCode + HashSecret qua email
    public class VnPayOptions
    {
        public string TmnCode { get; set; } = "";
        public string HashSecret { get; set; } = "";
        public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public int ExpireMinutes { get; set; } = 15;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(TmnCode) && !string.IsNullOrWhiteSpace(HashSecret);
    }

    // Tạo link thanh toán và kiểm tra chữ ký VNPay (theo tài liệu VNPay phiên bản 2.1.0)
    public static class VnPayHelper
    {
        // Ghép tham số theo thứ tự tên (a-z), mã hóa URL, rồi ký HMAC-SHA512 bằng HashSecret
        public static string CreatePaymentUrl(VnPayOptions options, IDictionary<string, string> parameters)
        {
            var query = BuildQuery(parameters);
            var secureHash = HmacSha512(options.HashSecret, query);
            return $"{options.BaseUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        // VNPay trả kết quả kèm vnp_SecureHash -> tự ký lại các tham số còn lại và so sánh.
        // Khớp = dữ liệu thật từ VNPay, không bị ai sửa số tiền / kết quả trên đường đi.
        public static bool ValidateSignature(VnPayOptions options, IDictionary<string, string> response)
        {
            if (!response.TryGetValue("vnp_SecureHash", out var receivedHash) || string.IsNullOrEmpty(receivedHash))
                return false;

            var data = response
                .Where(kv => kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var expected = HmacSha512(options.HashSecret, BuildQuery(data));
            return string.Equals(expected, receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildQuery(IDictionary<string, string> parameters)
        {
            return string.Join("&", parameters
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value)));
        }

        public static string HmacSha512(string key, string input)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }

        // Ý nghĩa một số mã lỗi vnp_ResponseCode thường gặp
        public static string ResponseMessage(string? code) => code switch
        {
            "00" => "Giao dịch thành công",
            "07" => "Trừ tiền thành công, giao dịch bị nghi ngờ",
            "09" => "Thẻ/Tài khoản chưa đăng ký Internet Banking",
            "10" => "Xác thực thông tin thẻ sai quá 3 lần",
            "11" => "Hết thời gian chờ thanh toán",
            "12" => "Thẻ/Tài khoản bị khóa",
            "13" => "Nhập sai mật khẩu OTP",
            "24" => "Khách hàng hủy giao dịch",
            "51" => "Tài khoản không đủ số dư",
            "65" => "Tài khoản vượt hạn mức giao dịch trong ngày",
            "75" => "Ngân hàng đang bảo trì",
            "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định",
            _ => $"Giao dịch không thành công (mã {code})"
        };
    }
}
