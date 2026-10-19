namespace Hotel_Web.Models.DTO
{
    public class RegisterDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string[] Roles { get; set; } = new[] { "Customer" };
        public string? FullName { get; set; }
        public string? Phone { get; set; }
    }

    public class LoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ChangePasswordDTO
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    public class LoginResponseDTO
    {
        public string JwtToken { get; set; }
    }
}