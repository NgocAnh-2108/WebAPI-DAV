using Microsoft.Extensions.Options;
using WebAPI_DAV.Data;
using WebAPI_DAV.Helpers;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public class SQLRoomRepository : IRoomRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly HotelPolicyOptions _policy;
        public SQLRoomRepository(AppDbContext dbContext, IOptions<HotelPolicyOptions> policy)
        {
            _dbContext = dbContext;
            _policy = policy.Value;
        }

        // Phòng đang có khách lưu trú (booking CheckedIn) -> không được đổi trạng thái dọn dẹp / bảo trì
        public bool IsOccupied(int roomId)
        {
            return _dbContext.BookingRooms.Any(br => br.RoomId == roomId && br.Booking.Status == "CheckedIn");
        }

        public List<RoomDTO> GetAll(string? filterOn = null, string? filterQuery = null,
            string? sortBy = null, bool isAscending = true,
            int pageNumber = 1, int pageSize = 1000)
        {
            var allRooms = _dbContext.Rooms.Select(r => new RoomDTO
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Status = r.Status,
                ImageUrl = _dbContext.Images
            .Where(i => i.RoomId == r.Id)
            .OrderByDescending(i => i.Id)
            .Select(i => i.FilePath)
            .FirstOrDefault(),
                Floor = r.Floor,
                RoomTypeName = r.RoomType.Name,
                BasePrice = r.RoomType.BasePrice
            }).AsQueryable();
            //filtering
            if (string.IsNullOrWhiteSpace(filterOn) == false && string.IsNullOrWhiteSpace(filterQuery) == false)
            {
                if (filterOn.Equals("roomnumber", StringComparison.OrdinalIgnoreCase))
                {
                    allRooms = allRooms.Where(x => x.RoomNumber.Contains(filterQuery));
                }
                else if (filterOn.Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    allRooms = allRooms.Where(x => x.Status.Contains(filterQuery));
                }
            }

            //sorting
            if (string.IsNullOrWhiteSpace(sortBy) == false)
            {
                if (sortBy.Equals("roomnumber", StringComparison.OrdinalIgnoreCase))
                {
                    allRooms = isAscending ? allRooms.OrderBy(x => x.RoomNumber) : allRooms.OrderByDescending(x => x.RoomNumber);
                }
                else if (sortBy.Equals("price", StringComparison.OrdinalIgnoreCase))
                {
                    allRooms = isAscending ? allRooms.OrderBy(x => x.BasePrice) : allRooms.OrderByDescending(x => x.BasePrice);
                }
            }

            //pagination
            var skipResults = (pageNumber - 1) * pageSize;
            return allRooms.Skip(skipResults).Take(pageSize).ToList();
        }

        public RoomDTO? GetById(int id)
        {
            return _dbContext.Rooms.Where(r => r.Id == id).Select(r => new RoomDTO
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Status = r.Status,
                ImageUrl = _dbContext.Images
                    .Where(i => i.RoomId == r.Id)
                    .OrderByDescending(i => i.Id)
                    .Select(i => i.FilePath)
                    .FirstOrDefault(),
                Floor = r.Floor,
                RoomTypeName = r.RoomType.Name,
                BasePrice = r.RoomType.BasePrice
            }).FirstOrDefault();
        }

        public AddRoomRequestDTO Add(AddRoomRequestDTO dto)
        {
            var domain = new Room
            {
                RoomNumber = dto.RoomNumber,
                Status = dto.Status,
                ImageUrl = dto.ImageUrl,
                Floor = dto.Floor,
                RoomTypeId = dto.RoomTypeId
            };
            _dbContext.Rooms.Add(domain);
            _dbContext.SaveChanges();
            return dto;
        }

        public AddRoomRequestDTO? Update(int id, AddRoomRequestDTO dto)
        {
            var domain = _dbContext.Rooms.FirstOrDefault(x => x.Id == id);
            if (domain == null) return null;
            domain.RoomNumber = dto.RoomNumber;
            domain.Status = dto.Status;
            domain.ImageUrl = dto.ImageUrl;
            domain.Floor = dto.Floor;
            domain.RoomTypeId = dto.RoomTypeId;
            _dbContext.SaveChanges();
            return dto;
        }

        public Room? Delete(int id)
        {
            var domain = _dbContext.Rooms.FirstOrDefault(x => x.Id == id);
            if (domain != null)
            {
                _dbContext.Rooms.Remove(domain);
                _dbContext.SaveChanges();
            }
            return domain;
        }
        public List<RoomAvailableDTO> GetAvailableRooms(DateTime checkIn, DateTime checkOut, int? roomTypeId = null)
        {
            // Khách chỉ chọn ngày -> tính theo giờ nhận 14:00 / trả 12:00 (đổi trong appsettings.json)
            checkIn = _policy.ApplyCheckInTime(checkIn);
            checkOut = _policy.ApplyCheckOutTime(checkOut);
            var nights = (checkOut.Date - checkIn.Date).Days;
            if (nights < 1)
                return new List<RoomAvailableDTO>();

            // lấy id các phòng đã bị đặt trùng khung giờ (booking chưa hủy / chưa trả / không phải no-show)
            var bookedRoomIds = _dbContext.BookingRooms
                .Where(br => br.Booking.Status != "Cancelled" && br.Booking.Status != "CheckedOut" && br.Booking.Status != "NoShow"
                    && checkIn < br.Booking.CheckOutDate && checkOut > br.Booking.CheckInDate)
                .Select(br => br.RoomId)
                .Distinct()
                .ToList();

            var query = _dbContext.Rooms
                .Where(r => r.Status != "Maintenance" && !bookedRoomIds.Contains(r.Id));

            if (roomTypeId.HasValue)
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);

            var result = query.Select(r => new RoomAvailableDTO
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                RoomTypeName = r.RoomType.Name,
                BasePrice = r.RoomType.BasePrice,
                MaxOccupancy = r.RoomType.MaxOccupancy,
                Nights = nights,
                TotalPriceForStay = r.RoomType.BasePrice * nights,
                ImageUrls = _dbContext.Images.Where(i => i.RoomId == r.Id).Select(i => i.FilePath).ToList()
            }).ToList();

            return result;
        }
    }
}