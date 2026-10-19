using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.Domain
{
    // Dịch vụ khách sử dụng trong 1 booking (bảng trung gian n-n Booking - Service, có thêm số lượng & đơn giá)
    public class BookingService
    {
        [Key]
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int ServiceId { get; set; }
        public Service Service { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }      // lưu giá tại thời điểm dùng (giá dịch vụ đổi sau này không ảnh hưởng hóa đơn cũ)
        public DateTime UsedDate { get; set; }
        public string? Note { get; set; }
    }
}
