namespace Hotel_Web.Models.DTO
{
    public class RoomAvailableDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
        public int MaxOccupancy { get; set; }
        public int Nights { get; set; }
        public decimal TotalPriceForStay { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public string? Status { get; set; }
        public string? Description { get; set; }
        public int Floor { get; set; }
    }

    // DTO khớp với response thật của GET /api/Rooms/get-all-rooms (RoomDTO bên API)
    public class RoomRawDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public string Status { get; set; }
        public string? ImageUrl { get; set; }
        public int Floor { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
    }
}