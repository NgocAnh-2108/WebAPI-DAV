using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.DTO
{
    public class ServiceDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
    }

    public class AddServiceRequestDTO
    {
        [Required]
        [MinLength(2)]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Required]
        public string Category { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        public decimal Price { get; set; }
        [Required]
        public string Unit { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class BookingServiceDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public string? CustomerName { get; set; }
        public List<string> RoomNumbers { get; set; } = new();
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public DateTime UsedDate { get; set; }
        public string? Note { get; set; }
    }

    public class AddBookingServiceRequestDTO
    {
        [Required]
        public int BookingId { get; set; }
        [Required]
        public int ServiceId { get; set; }
        [Range(1, 1000, ErrorMessage = "Số lượng từ 1 đến 1000")]
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }
}
