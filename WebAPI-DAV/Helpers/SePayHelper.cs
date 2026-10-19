using System.Net;
using System.Text.RegularExpressions;

namespace WebAPI_DAV.Helpers
{
    // Cấu hình nhận tiền chuyển khoản qua tài khoản ngân hàng (MB Bank) + SePay báo biến động số dư
    // Đọc từ appsettings.json, mục "SePay"
    public class SePayOptions
    {
        public string ApiKey { get; set; } = "";          // khóa bí mật nhập ở bước "Bảo mật" khi tạo webhook trên SePay
        public string BankCode { get; set; } = "MB";       // mã ngân hàng theo VietQR (MB = MB Bank)
        public string AccountNumber { get; set; } = "";    // số tài khoản nhận tiền
        public string AccountName { get; set; } = "";      // tên chủ tài khoản (in hoa, không dấu)

        public bool IsConfigured => !string.IsNullOrWhiteSpace(AccountNumber);
    }

    public static class SePayHelper
    {
        // Nội dung chuyển khoản = mã booking bỏ dấu "-" (BK-7F3A9C21 -> BK7F3A9C21)
        // vì nhiều ngân hàng tự xóa ký tự đặc biệt trong nội dung chuyển
        public static string PaymentContent(string bookingCode) => bookingCode.Replace("-", "").ToUpper();

        // Ảnh QR VietQR: quét bằng app ngân hàng bất kỳ (hoặc MoMo) là tự điền sẵn số TK, số tiền, nội dung
        public static string QrImageUrl(SePayOptions o, decimal amount, string content) =>
            $"https://img.vietqr.io/image/{o.BankCode}-{o.AccountNumber}-compact2.png" +
            $"?amount={(long)amount}&addInfo={WebUtility.UrlEncode(content)}&accountName={WebUtility.UrlEncode(o.AccountName)}";

        // Tìm mã booking trong nội dung chuyển khoản: "BK" + 8 ký tự (chấp nhận cả có/không dấu "-", chữ thường)
        public static string? FindBookingCode(params string?[] texts)
        {
            foreach (var t in texts)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                var m = Regex.Match(t.ToUpper(), @"BK[\s\-_]?([0-9A-F]{8})");
                if (m.Success) return "BK-" + m.Groups[1].Value;
            }
            return null;
        }
    }
}
