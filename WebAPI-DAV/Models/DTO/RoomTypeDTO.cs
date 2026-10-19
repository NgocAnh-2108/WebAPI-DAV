using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.DTO
{
    public class RoomTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int MaxOccupancy { get; set; }
    }

    public class AddRoomTypeRequestDTO
    {
        [Required]
        [MinLength(3)]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "BasePrice phải >= 0")]
        public decimal BasePrice { get; set; }
        [Range(1, 20, ErrorMessage = "MaxOccupancy từ 1 đến 20")]
        public int MaxOccupancy { get; set; }
    }
}