using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.Domain
{
    public class Room
    {
        [Key]
        public int Id { get; set; }
        public string RoomNumber { get; set; }        // VD: 101, 202
        public string Status { get; set; }             // Available, Booked, Maintenance
        public string? ImageUrl { get; set; }
        public int Floor { get; set; }

        // Navigation - Room thuộc 1 RoomType
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }

        // Navigation - 1 Room có nhiều BookingRoom
        public List<BookingRoom> BookingRooms { get; set; }
    }
}