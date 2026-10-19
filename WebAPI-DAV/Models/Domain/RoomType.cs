using System.ComponentModel.DataAnnotations;

namespace WebAPI_DAV.Models.Domain
{
    public class RoomType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }           // VD: Phòng đơn, Phòng đôi, Suite
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }       // giá cơ bản/đêm
        public int MaxOccupancy { get; set; }         // số khách tối đa

        // Navigation - 1 RoomType có nhiều Room
        public List<Room> Rooms { get; set; }
    }
}