using Microsoft.EntityFrameworkCore;
using WebAPI_DAV.Data;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    // Kho chuyển phòng: khách đang ở gặp sự cố -> đổi sang phòng khác
    // Quy tắc: ưu tiên phòng CÙNG HẠNG; hết phòng cùng hạng mới chuyển xuống hạng THẤP HƠN
    // và trừ tiền chênh lệch (giá cũ - giá mới) x số đêm còn lại vào hóa đơn.
    // Không còn cả 2 loại trên thì cho nâng hạng miễn phí (lỗi do khách sạn, khách không trả thêm).
    public class SQLRoomTransferRepository : IRoomTransferRepository
    {
        private readonly AppDbContext _dbContext;

        public SQLRoomTransferRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private Booking? LoadBooking(int bookingId) => _dbContext.Bookings
            .Include(b => b.Customer)
            .Include(b => b.BookingRooms).ThenInclude(br => br.Room).ThenInclude(r => r.RoomType)
            .FirstOrDefault(b => b.Id == bookingId);

        // Số đêm còn lại: khách đang ở -> tính từ đêm nay; chưa nhận phòng -> toàn bộ số đêm
        private static int RemainingNights(Booking b)
        {
            var total = Math.Max(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days);
            if (b.Status != "CheckedIn") return total;
            var fromDay = DateTime.Today > b.CheckInDate.Date ? DateTime.Today : b.CheckInDate.Date;
            return Math.Clamp((b.CheckOutDate.Date - fromDay).Days, 0, total);
        }

        public (bool Success, string Message, TransferOptionsResultDTO? Result) GetOptions(int bookingId, int? fromRoomId)
        {
            var booking = LoadBooking(bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng", null);
            if (booking.Status != "CheckedIn" && booking.Status != "Confirmed")
                return (false, "Chỉ chuyển phòng cho khách đang lưu trú hoặc đã xác nhận đặt phòng", null);

            var fromLink = fromRoomId.HasValue && fromRoomId.Value > 0
                ? booking.BookingRooms.FirstOrDefault(br => br.RoomId == fromRoomId.Value)
                : booking.BookingRooms.FirstOrDefault();
            if (fromLink == null) return (false, "Phòng cần chuyển không thuộc booking này", null);
            var fromRoom = fromLink.Room;
            var fromPrice = fromRoom.RoomType.BasePrice;

            // Khoảng thời gian phòng mới phải trống: từ bây giờ (hoặc giờ nhận phòng) tới giờ trả phòng
            var start = booking.Status == "CheckedIn" && DateTime.Now > booking.CheckInDate ? DateTime.Now : booking.CheckInDate;
            var end = booking.CheckOutDate;
            var nights = RemainingNights(booking);
            var currentRoomIds = booking.BookingRooms.Select(br => br.RoomId).ToList();

            var busyRoomIds = _dbContext.BookingRooms
                .Where(br => br.BookingId != booking.Id
                    && br.Booking.Status != "Cancelled" && br.Booking.Status != "CheckedOut" && br.Booking.Status != "NoShow"
                    && start < br.Booking.CheckOutDate && end > br.Booking.CheckInDate)
                .Select(br => br.RoomId).Distinct().ToList();

            var freeRooms = _dbContext.Rooms.Include(r => r.RoomType)
                .Where(r => r.Status != "Maintenance" && !currentRoomIds.Contains(r.Id) && !busyRoomIds.Contains(r.Id))
                .ToList();

            string TierOf(Room r) => r.RoomTypeId == fromRoom.RoomTypeId || r.RoomType.BasePrice == fromPrice ? "Same"
                : r.RoomType.BasePrice < fromPrice ? "Lower" : "Higher";

            var all = freeRooms.Select(r => new TransferOptionDTO
            {
                RoomId = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomType.Name,
                BasePrice = r.RoomType.BasePrice,
                Tier = TierOf(r),
                EstimatedDiscount = TierOf(r) == "Lower" ? (fromPrice - r.RoomType.BasePrice) * nights : 0
            }).ToList();

            var result = new TransferOptionsResultDTO
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer.FullName,
                FromRoomId = fromRoom.Id,
                FromRoomNumber = fromRoom.RoomNumber,
                FromRoomTypeName = fromRoom.RoomType.Name,
                FromPrice = fromPrice,
                RemainingNights = nights,
                CurrentRooms = booking.BookingRooms.Select(br => new BookingRoomInfoDTO
                {
                    RoomId = br.RoomId,
                    RoomNumber = br.Room.RoomNumber,
                    RoomTypeName = br.Room.RoomType.Name,
                    BasePrice = br.Room.RoomType.BasePrice
                }).ToList()
            };

            if (all.Any(o => o.Tier == "Same"))
            {
                result.Policy = "Same";
                result.PolicyMessage = "Còn phòng cùng hạng — chuyển khách sang phòng cùng hạng, giữ nguyên giá";
                result.Options = all.Where(o => o.Tier == "Same").OrderBy(o => o.Floor).ThenBy(o => o.RoomNumber).ToList();
            }
            else if (all.Any(o => o.Tier == "Lower"))
            {
                result.Policy = "Lower";
                result.PolicyMessage = $"Hết phòng cùng hạng — chuyển xuống hạng thấp hơn, trừ tiền chênh lệch {nights} đêm còn lại vào hóa đơn";
                result.Options = all.Where(o => o.Tier == "Lower").OrderByDescending(o => o.BasePrice).ThenBy(o => o.RoomNumber).ToList();
            }
            else if (all.Any(o => o.Tier == "Higher"))
            {
                result.Policy = "Higher";
                result.PolicyMessage = "Hết phòng cùng hạng và hạng thấp hơn — nâng hạng miễn phí cho khách";
                result.Options = all.Where(o => o.Tier == "Higher").OrderBy(o => o.BasePrice).ThenBy(o => o.RoomNumber).ToList();
            }
            else
            {
                result.Policy = "None";
                result.PolicyMessage = "Không còn phòng trống nào trong thời gian lưu trú còn lại của khách";
            }
            return (true, "OK", result);
        }

        public (bool Success, string Message) Transfer(int bookingId, TransferRoomRequestDTO dto)
        {
            // Lấy lại danh sách phòng hợp lệ để chắc chắn phòng được chọn đúng chính sách
            var (ok, message, options) = GetOptions(bookingId, dto.FromRoomId);
            if (!ok || options == null) return (false, message);

            var target = options.Options.FirstOrDefault(o => o.RoomId == dto.ToRoomId);
            if (target == null)
                return (false, options.Policy == "Same"
                    ? "Vẫn còn phòng cùng hạng — phải ưu tiên chuyển sang phòng cùng hạng"
                    : "Phòng được chọn không còn trống hoặc không đúng chính sách chuyển phòng");

            var booking = LoadBooking(bookingId)!;
            var link = booking.BookingRooms.First(br => br.RoomId == dto.FromRoomId);
            var oldRoom = link.Room;

            // Đổi phòng trong booking
            var newRoom = _dbContext.Rooms.First(r => r.Id == target.RoomId);
            link.Room = newRoom;
            link.RoomId = newRoom.Id;

            // Giảm trừ khi xuống hạng
            var discount = target.Tier == "Lower" ? target.EstimatedDiscount : 0;
            if (discount > 0)
            {
                booking.RoomChangeDiscount += discount;
                booking.TotalPrice = Math.Max(0, booking.TotalPrice - discount);
            }

            // Phòng cũ gặp sự cố -> đưa vào bảo trì để không ai đặt tiếp
            if (dto.MarkOldRoomMaintenance)
                oldRoom.Status = "Maintenance";

            _dbContext.RoomTransfers.Add(new RoomTransfer
            {
                BookingId = booking.Id,
                FromRoomId = oldRoom.Id,
                ToRoomId = target.RoomId,
                OldPricePerNight = options.FromPrice,
                NewPricePerNight = target.BasePrice,
                RemainingNights = options.RemainingNights,
                DiscountAmount = discount,
                Reason = dto.Reason,
                CreatedDate = DateTime.Now
            });
            _dbContext.SaveChanges();

            var msg = $"Đã chuyển khách từ phòng {oldRoom.RoomNumber} sang phòng {target.RoomNumber} ({target.RoomTypeName})";
            if (discount > 0) msg += $". Giảm trừ {discount:N0}đ vào hóa đơn";
            if (target.Tier == "Higher") msg += ". Nâng hạng miễn phí";
            return (true, msg);
        }

        public List<RoomTransferDTO> GetByBooking(int bookingId)
        {
            return _dbContext.RoomTransfers
                .Where(t => t.BookingId == bookingId)
                .OrderBy(t => t.CreatedDate)
                .Select(t => new RoomTransferDTO
                {
                    Id = t.Id,
                    BookingId = t.BookingId,
                    FromRoomNumber = t.FromRoom.RoomNumber,
                    ToRoomNumber = t.ToRoom.RoomNumber,
                    OldPricePerNight = t.OldPricePerNight,
                    NewPricePerNight = t.NewPricePerNight,
                    RemainingNights = t.RemainingNights,
                    DiscountAmount = t.DiscountAmount,
                    Reason = t.Reason,
                    CreatedDate = t.CreatedDate
                }).ToList();
        }
    }
}
