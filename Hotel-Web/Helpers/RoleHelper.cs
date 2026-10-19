using System.IdentityModel.Tokens.Jwt;

namespace Hotel_Web.Helpers
{
    // Đọc role từ JWT lưu trong Session — dùng chung cho Controller và View
    public static class RoleHelper
    {
        public static List<string> GetRoles(string? token)
        {
            if (string.IsNullOrEmpty(token)) return new List<string>();
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                return jwt.Claims
                    .Where(c => c.Type == "role" || c.Type.EndsWith("/role"))
                    .Select(c => c.Value)
                    .Distinct()
                    .ToList();
            }
            catch { return new List<string>(); }
        }

        public static bool IsExpired(string? token)
        {
            if (string.IsNullOrEmpty(token)) return true;
            try { return new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo < DateTime.UtcNow; }
            catch { return true; }
        }

        // Role "chính" để quyết định giao diện: Manager > Receptionist > Customer
        public static string? MainRole(ISession session)
        {
            var roles = GetRoles(session.GetString("JwtToken"));
            if (roles.Contains("Manager")) return "Manager";
            if (roles.Contains("Receptionist")) return "Receptionist";
            if (roles.Contains("Customer")) return "Customer";
            return null;
        }

        public static bool IsStaff(ISession session)
        {
            var r = MainRole(session);
            return r == "Manager" || r == "Receptionist";
        }

        public static string RoleLabel(string? role) => role switch
        {
            "Manager" => "Quản lý",
            "Receptionist" => "Lễ tân",
            "Customer" => "Khách hàng",
            _ => ""
        };

        public static string StatusLabel(string? status) => status switch
        {
            "Pending" => "Chờ thanh toán cọc",
            "Confirmed" => "Đã xác nhận",
            "CheckedIn" => "Đang lưu trú",
            "CheckedOut" => "Đã trả phòng",
            "Cancelled" => "Đã hủy",
            "NoShow" => "Không đến",
            "Available" => "Sẵn sàng",
            "Maintenance" => "Bảo trì",
            "Occupied" => "Có khách",
            "Reserved" => "Đã đặt trước",
            "Success" => "Thành công",
            "Failed" => "Thất bại",
            "Underpaid" => "Chuyển thiếu",
            _ => status ?? ""
        };
    }
}
