using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.Domain
{
    // Dịch vụ khách sạn: ăn sáng, spa, giặt ủi, đưa đón sân bay...
    public class Service
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }        // Ẩm thực, Spa & Thư giãn, Giặt ủi, Di chuyển, Khác
        public decimal Price { get; set; }
        public string Unit { get; set; }            // lần, suất, kg, chuyến, giờ...
        public bool IsActive { get; set; } = true;  // ngừng cung cấp thì ẩn đi thay vì xóa (giữ lịch sử)

        // Navigation - 1 Service được dùng trong nhiều BookingService
        public List<BookingService> BookingServices { get; set; }
    }
}
