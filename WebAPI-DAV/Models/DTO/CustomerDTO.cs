using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.DTO
{
    public class CustomerDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
    }

    public class AddCustomerRequestDTO
    {
        [Required]
        [MinLength(3)]
        public string FullName { get; set; }
        [Required]
        [Phone]
        public string Phone { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
    }
}