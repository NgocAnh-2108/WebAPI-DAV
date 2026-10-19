using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.Domain
{
    // Lịch sử chuyển phòng: khách đang ở gặp sự cố -> chuyển sang phòng khác
    public class RoomTransfer
    {
        [Key]
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int FromRoomId { get; set; }
        public Room FromRoom { get; set; }

        public int ToRoomId { get; set; }
        public Room ToRoom { get; set; }

        public decimal OldPricePerNight { get; set; }
        public decimal NewPricePerNight { get; set; }
        public int RemainingNights { get; set; }      // số đêm còn lại tính từ lúc chuyển
        public decimal DiscountAmount { get; set; }    // tiền giảm trừ = chênh lệch giá x số đêm còn lại
        public string? Reason { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
