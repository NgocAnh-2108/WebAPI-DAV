using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.DTO
{
    public class RegisterRequestDTO
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Username { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string[] Roles { get; set; }

        // chỉ cần khi đăng ký với role Customer
        public string? FullName { get; set; }
        public string? Phone { get; set; }
    }
}