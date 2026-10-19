namespace Hotel_Web.Models.DTO
{
    public class RoomTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int MaxOccupancy { get; set; }
    }

    public class AddRoomTypeDTO
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int MaxOccupancy { get; set; }
    }

    public class ManageRoomDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public string Status { get; set; }
        public string? ImageUrl { get; set; }
        public int Floor { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class AddRoomDTO
    {
        public string RoomNumber { get; set; } = "";
        public string Status { get; set; } = "Available";
        public string? ImageUrl { get; set; }
        public int Floor { get; set; }
        public int RoomTypeId { get; set; }
    }

    public class CustomerDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
    }

    public class StaffAccountDTO
    {
        public string Id { get; set; } = "";
        public bool CanDelete { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
    }

    public class RegisterStaffDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string? FullName { get; set; }
        public string? Phone { get; set; }
    }
}
namespace Hotel_Web.Models.DTO
{
    // Phòng + trạng thái "thực tế" hôm nay (tính từ booking)
    public class RoomLiveDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = "";
        public int Floor { get; set; }
        public string RoomTypeName { get; set; } = "";
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string RoomStatus { get; set; } = "";   // Available/Maintenance (cột trong DB)
        public string LiveStatus { get; set; } = "";   // Available/Reserved/Occupied/Maintenance
        public string? GuestName { get; set; }
        public string? BookingCode { get; set; }
        public bool HasGuest { get; set; }             // có booking đang CheckedIn ở phòng này
    }

    public class DashboardVM
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int ReservedRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public int OccupancyPercent { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingBookings { get; set; }
        public int ArrivalsToday { get; set; }
        public int DeparturesToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueTotal { get; set; }
        public List<BookingWithDetailDTO> RecentBookings { get; set; } = new();
        public List<BookingWithDetailDTO> TodayArrivals { get; set; } = new();
        public List<BookingWithDetailDTO> TodayDepartures { get; set; } = new();
        public List<(string Label, decimal Total)> RevenueByMonth { get; set; } = new();
    }

    public class PaymentListDTO
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; } = "";
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public string? CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Method { get; set; }
        public string? GatewayTransactionNo { get; set; }
        public string? BankCode { get; set; }
    }

    public class AddCustomerDTO
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
    }

    public class StaffBookingRequestDTO
    {
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Note { get; set; }
        public int CustomerID { get; set; }
        public List<int> RoomIds { get; set; } = new();
    }
}

namespace Hotel_Web.Models.DTO
{
    public class ServiceDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public string Unit { get; set; } = "";
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
    }

    public class AddServiceDTO
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public string Unit { get; set; } = "";
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
        public string ServiceName { get; set; } = "";
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public DateTime UsedDate { get; set; }
        public string? Note { get; set; }
    }

    public class AddBookingServiceDTO
    {
        public int BookingId { get; set; }
        public int ServiceId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }
}
