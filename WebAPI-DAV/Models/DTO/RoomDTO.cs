using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.DTO
{
    public class RoomDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public string Status { get; set; }
        public string? ImageUrl { get; set; }
        public int Floor { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class AddRoomRequestDTO
    {
        [Required]
        public string RoomNumber { get; set; }
        [Required]
        public string Status { get; set; } // Available, Booked, Maintenance
        public string? ImageUrl { get; set; }
        [Range(0, 200)]
        public int Floor { get; set; }
        public int RoomTypeId { get; set; }
    }
    public class RoomAvailableDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
        public int MaxOccupancy { get; set; }
        public int Nights { get; set; }
        public decimal TotalPriceForStay { get; set; }
        public List<string> ImageUrls { get; set; }
    }
}