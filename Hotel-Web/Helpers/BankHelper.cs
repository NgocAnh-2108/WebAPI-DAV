using System.Net;

namespace Hotel_Web.Helpers
{
    // Danh sách ngân hàng (mã theo chuẩn VietQR) để khách chọn tài khoản nhận tiền hoàn cọc
    public static class BankHelper
    {
        public static readonly List<(string Code, string Name)> Banks = new()
        {
            ("MB", "MB Bank"), ("VCB", "Vietcombank"), ("ICB", "VietinBank"), ("BIDV", "BIDV"),
            ("VBA", "Agribank"), ("TCB", "Techcombank"), ("ACB", "ACB"), ("VPB", "VPBank"),
            ("TPB", "TPBank"), ("STB", "Sacombank"), ("VIB", "VIB"), ("SHB", "SHB"),
            ("HDB", "HDBank"), ("OCB", "OCB"), ("MSB", "MSB"), ("SEAB", "SeABank"), ("LPB", "LPBank")
        };

        public static string Name(string? code) =>
            Banks.FirstOrDefault(b => b.Code == code).Name ?? code ?? "";

        // QR chuyển khoản tới tài khoản của khách: nhân viên mở app ngân hàng quét là hoàn tiền xong
        public static string RefundQr(string bankCode, string accountNumber, string accountName, decimal amount, string bookingCode) =>
            $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.png?amount={(long)amount}" +
            $"&addInfo={WebUtility.UrlEncode("HOAN COC " + bookingCode.Replace("-", ""))}&accountName={WebUtility.UrlEncode(accountName)}";
    }
}
