using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.Domain
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string? IdentityCard { get; set; }       // CCCD/CMND
        public string? Address { get; set; }
        public string? UserId { get; set; }// Id của IdentityUser (null nếu khách lễ tân tạo, không có tài khoản)

        // Navigation - 1 Customer có nhiều Booking
        public List<Booking> Bookings { get; set; }
    }
}