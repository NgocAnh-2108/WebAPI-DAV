using Microsoft.EntityFrameworkCore;
using WebAPI_DAV.Data;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public class SQLServiceRepository : IServiceRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLServiceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ================= DỊCH VỤ =================
        public List<ServiceDTO> GetAll(bool includeInactive = false, string? category = null)
        {
            var query = _dbContext.Services.AsQueryable();
            if (!includeInactive) query = query.Where(s => s.IsActive);
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(s => s.Category == category);

            return query.OrderBy(s => s.Category).ThenBy(s => s.Name).Select(s => new ServiceDTO
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                Price = s.Price,
                Unit = s.Unit,
                IsActive = s.IsActive,
                UsageCount = s.BookingServices.Count()
            }).ToList();
        }

        public ServiceDTO? GetById(int id)
        {
            return _dbContext.Services.Where(s => s.Id == id).Select(s => new ServiceDTO
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                Price = s.Price,
                Unit = s.Unit,
                IsActive = s.IsActive,
                UsageCount = s.BookingServices.Count()
            }).FirstOrDefault();
        }

        public AddServiceRequestDTO Add(AddServiceRequestDTO dto)
        {
            var domain = new Service
            {
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                Price = dto.Price,
                Unit = dto.Unit,
                IsActive = dto.IsActive
            };
            _dbContext.Services.Add(domain);
            _dbContext.SaveChanges();
            return dto;
        }

        public AddServiceRequestDTO? Update(int id, AddServiceRequestDTO dto)
        {
            var domain = _dbContext.Services.FirstOrDefault(s => s.Id == id);
            if (domain == null) return null;
            domain.Name = dto.Name;
            domain.Description = dto.Description;
            domain.Category = dto.Category;
            domain.Price = dto.Price;
            domain.Unit = dto.Unit;
            domain.IsActive = dto.IsActive;
            _dbContext.SaveChanges();
            return dto;
        }

        // Dịch vụ đã từng được dùng -> chỉ ngừng cung cấp (giữ hóa đơn cũ), chưa dùng -> xóa hẳn
        public (bool Success, string Message) Delete(int id)
        {
            var domain = _dbContext.Services.FirstOrDefault(s => s.Id == id);
            if (domain == null) return (false, "Không tìm thấy dịch vụ");

            if (_dbContext.BookingServices.Any(bs => bs.ServiceId == id))
            {
                domain.IsActive = false;
                _dbContext.SaveChanges();
                return (true, "Dịch vụ đã có khách sử dụng nên được chuyển sang 'Ngừng cung cấp'");
            }

            _dbContext.Services.Remove(domain);
            _dbContext.SaveChanges();
            return (true, "Đã xóa dịch vụ");
        }

        // ================= DỊCH VỤ KHÁCH SỬ DỤNG =================
        private IQueryable<BookingServiceDTO> ProjectUsages(IQueryable<BookingService> query)
        {
            return query.Select(bs => new BookingServiceDTO
            {
                Id = bs.Id,
                BookingId = bs.BookingId,
                BookingCode = bs.Booking.BookingCode,
                CustomerName = bs.Booking.Customer.FullName,
                RoomNumbers = bs.Booking.BookingRooms.Select(br => br.Room.RoomNumber).ToList(),
                ServiceId = bs.ServiceId,
                ServiceName = bs.Service.Name,
                Category = bs.Service.Category,
                Unit = bs.Service.Unit,
                Quantity = bs.Quantity,
                UnitPrice = bs.UnitPrice,
                Total = bs.UnitPrice * bs.Quantity,
                UsedDate = bs.UsedDate,
                Note = bs.Note
            });
        }

        public List<BookingServiceDTO> GetUsages(int? bookingId = null, DateTime? from = null, DateTime? to = null)
        {
            var query = _dbContext.BookingServices.Where(bs => bs.Booking.Status != "Cancelled" && bs.Booking.Status != "NoShow");
            if (bookingId.HasValue) query = query.Where(bs => bs.BookingId == bookingId.Value);
            if (from.HasValue) query = query.Where(bs => bs.UsedDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(bs => bs.UsedDate < to.Value.Date.AddDays(1));
            return ProjectUsages(query.OrderByDescending(bs => bs.UsedDate)).ToList();
        }

        public List<BookingServiceDTO> GetUsagesForUser(string userId)
        {
            var query = _dbContext.BookingServices.Where(bs => bs.Booking.Customer.UserId == userId);
            return ProjectUsages(query.OrderByDescending(bs => bs.UsedDate)).ToList();
        }

        public (bool Success, string Message, BookingServiceDTO? Result) AddUsage(AddBookingServiceRequestDTO dto)
        {
            var booking = _dbContext.Bookings.FirstOrDefault(b => b.Id == dto.BookingId);
            if (booking == null) return (false, "Không tìm thấy booking", null);

            // chỉ ghi dịch vụ cho khách đã xác nhận / đang lưu trú
            if (booking.Status != "CheckedIn" && booking.Status != "Confirmed")
                return (false, $"Không thể thêm dịch vụ khi booking đang ở trạng thái '{booking.Status}'", null);

            var service = _dbContext.Services.FirstOrDefault(s => s.Id == dto.ServiceId);
            if (service == null) return (false, "Không tìm thấy dịch vụ", null);
            if (!service.IsActive) return (false, "Dịch vụ này đã ngừng cung cấp", null);

            var domain = new BookingService
            {
                BookingId = dto.BookingId,
                ServiceId = dto.ServiceId,
                Quantity = dto.Quantity,
                UnitPrice = service.Price,   // giá lấy từ server, không tin client
                UsedDate = DateTime.Now,
                Note = dto.Note
            };
            _dbContext.BookingServices.Add(domain);
            _dbContext.SaveChanges();

            var result = ProjectUsages(_dbContext.BookingServices.Where(bs => bs.Id == domain.Id)).FirstOrDefault();
            return (true, $"Đã thêm {dto.Quantity} {service.Unit} {service.Name}", result);
        }

        public (bool Success, string Message) DeleteUsage(int id)
        {
            var domain = _dbContext.BookingServices.Include(bs => bs.Booking).FirstOrDefault(bs => bs.Id == id);
            if (domain == null) return (false, "Không tìm thấy bản ghi");
            if (domain.Booking.Status == "CheckedOut")
                return (false, "Booking đã trả phòng (đã xuất hóa đơn), không thể xóa dịch vụ");

            _dbContext.BookingServices.Remove(domain);
            _dbContext.SaveChanges();
            return (true, "Đã xóa dịch vụ khỏi booking");
        }
    }
}
