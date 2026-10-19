using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAPI_DAV.Data;
using WebAPI_DAV.Helpers;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public class SQLBookingRepository : IBookingRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly HotelPolicyOptions _policy;
        private const decimal DepositRate = 0.3m; // cọc 30%

        public SQLBookingRepository(AppDbContext dbContext, IOptions<HotelPolicyOptions> policy)
        {
            _dbContext = dbContext;
            _policy = policy.Value;
        }

        // Chuyển Booking (Domain) -> BookingWithDetailDTO, dùng chung cho mọi hàm đọc dữ liệu
        private static readonly Expression<Func<Booking, BookingWithDetailDTO>> ToDto = b => new BookingWithDetailDTO
        {
            Id = b.Id,
            BookingCode = b.BookingCode,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            Status = b.Status,
            TotalPrice = b.TotalPrice,
            DepositAmount = b.DepositAmount,
            IsDepositPaid = b.IsDepositPaid,
            CreatedDate = b.CreatedDate,
            Note = b.Note,
            CustomerId = b.CustomerID,
            CustomerName = b.Customer.FullName,
            CustomerPhone = b.Customer.Phone,
            RoomNumbers = b.BookingRooms.Select(br => br.Room.RoomNumber).ToList(),
            Rooms = b.BookingRooms.Select(br => new BookingRoomInfoDTO
            {
                RoomId = br.RoomId,
                RoomNumber = br.Room.RoomNumber,
                RoomTypeName = br.Room.RoomType.Name,
                BasePrice = br.Room.RoomType.BasePrice
            }).ToList(),
            CancelledDate = b.CancelledDate,
            CancelReason = b.CancelReason,
            CancellationFee = b.CancellationFee,
            RefundAmount = b.RefundAmount,
            RefundStatus = b.RefundStatus,
            RefundBankCode = b.RefundBankCode,
            RefundAccountNumber = b.RefundAccountNumber,
            RefundAccountName = b.RefundAccountName,
            RefundedDate = b.RefundedDate,
            RefundNote = b.RefundNote,
            RoomChangeDiscount = b.RoomChangeDiscount
        };

        // Phòng có bị trùng lịch không: booking còn hiệu lực (chưa hủy / chưa trả / không phải no-show)
        // và 2 khoảng thời gian giao nhau. Có tính cả GIỜ nên trả 12:00 - nhận 14:00 cùng ngày không bị trùng.
        private bool IsRoomBusy(int roomId, DateTime from, DateTime to, int? exceptBookingId = null)
        {
            return _dbContext.BookingRooms
                .Where(br => br.RoomId == roomId && br.BookingId != (exceptBookingId ?? 0))
                .Any(br => br.Booking.Status != "Cancelled" && br.Booking.Status != "CheckedOut" && br.Booking.Status != "NoShow"
                    && from < br.Booking.CheckOutDate && to > br.Booking.CheckInDate);
        }

        private static int CountNights(DateTime checkIn, DateTime checkOut) => (checkOut.Date - checkIn.Date).Days;

        // ================= KHÁCH TỰ ĐẶT PHÒNG =================
        public (bool Success, string Message, BookingWithDetailDTO? Result) AddForCustomer(
            CustomerBookingRequestDTO dto, string userId, ICustomerRepository customerRepository)
        {
            var customer = customerRepository.GetByUserId(userId);
            if (customer == null)
                return (false, "Không tìm thấy hồ sơ khách hàng, vui lòng liên hệ lễ tân", null);

            // Khách chỉ chọn ngày -> gắn giờ nhận/trả mặc định (14:00 / 12:00)
            var checkIn = _policy.ApplyCheckInTime(dto.CheckInDate);
            var checkOut = _policy.ApplyCheckOutTime(dto.CheckOutDate);

            var nights = CountNights(checkIn, checkOut);
            if (nights < 1)
                return (false, "Ngày trả phòng phải sau ngày nhận phòng", null);
            if (checkIn < DateTime.Now.AddHours(-12))
                return (false, "Ngày nhận phòng đã qua", null);

            decimal totalPrice = 0;
            foreach (var roomId in dto.RoomIds)
            {
                var room = _dbContext.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.Id == roomId);
                if (room == null)
                    return (false, $"Không tìm thấy phòng id {roomId}", null);
                if (room.Status == "Maintenance")
                    return (false, $"Phòng {room.RoomNumber} đang bảo trì", null);
                if (IsRoomBusy(roomId, checkIn, checkOut))
                    return (false, $"Phòng {room.RoomNumber} đã được đặt trong khoảng thời gian này", null);

                totalPrice += room.RoomType.BasePrice * nights;
            }

            // Kiểm tra dịch vụ khách chọn kèm (trước khi lưu booking để không tạo booking dở dang)
            var serviceItems = (dto.Services ?? new List<ServiceItemDTO>()).Where(s => s.Quantity > 0).ToList();
            var serviceIds = serviceItems.Select(s => s.ServiceId).ToList();
            var services = _dbContext.Services.Where(s => serviceIds.Contains(s.Id) && s.IsActive).ToList();
            if (services.Count != serviceIds.Distinct().Count())
                return (false, "Có dịch vụ không tồn tại hoặc đã ngừng cung cấp", null);

            var bookingDomain = new Booking
            {
                BookingCode = NewBookingCode(),
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Status = "Pending",
                TotalPrice = totalPrice,
                DepositAmount = Math.Round(totalPrice * DepositRate, 0),
                IsDepositPaid = false,
                CreatedDate = DateTime.Now,
                Note = dto.Note,
                CustomerID = customer.Id
            };
            _dbContext.Bookings.Add(bookingDomain);
            _dbContext.SaveChanges();

            foreach (var roomId in dto.RoomIds)
                _dbContext.BookingRooms.Add(new BookingRoom { BookingId = bookingDomain.Id, RoomId = roomId });

            // Lưu dịch vụ đặt kèm — giá lấy từ DB, ngày sử dụng = ngày nhận phòng
            foreach (var item in serviceItems)
            {
                var service = services.First(s => s.Id == item.ServiceId);
                _dbContext.BookingServices.Add(new BookingService
                {
                    BookingId = bookingDomain.Id,
                    ServiceId = service.Id,
                    Quantity = item.Quantity,
                    UnitPrice = service.Price,
                    UsedDate = checkIn,
                    Note = "Khách đặt kèm khi đặt phòng"
                });
            }
            _dbContext.SaveChanges();

            return (true, "Đặt phòng thành công. Vui lòng thanh toán cọc để xác nhận đặt phòng", GetById(bookingDomain.Id));
        }

        public List<BookingWithDetailDTO> GetMyBookings(string userId)
        {
            return _dbContext.Bookings
                .Where(b => b.Customer.UserId == userId)
                .OrderByDescending(b => b.CreatedDate)
                .Select(ToDto).ToList();
        }

        public List<BookingWithDetailDTO> GetAll(string? filterOn = null, string? filterQuery = null,
            string? sortBy = null, bool isAscending = true,
            int pageNumber = 1, int pageSize = 1000)
        {
            var allBookings = _dbContext.Bookings.AsQueryable();

            //filtering
            if (string.IsNullOrWhiteSpace(filterOn) == false && string.IsNullOrWhiteSpace(filterQuery) == false)
            {
                if (filterOn.Equals("status", StringComparison.OrdinalIgnoreCase))
                    allBookings = allBookings.Where(x => x.Status.Contains(filterQuery));
                else if (filterOn.Equals("customername", StringComparison.OrdinalIgnoreCase))
                    allBookings = allBookings.Where(x => x.Customer.FullName.Contains(filterQuery));
            }

            //sorting
            if (string.IsNullOrWhiteSpace(sortBy) == false)
            {
                if (sortBy.Equals("checkindate", StringComparison.OrdinalIgnoreCase))
                    allBookings = isAscending ? allBookings.OrderBy(x => x.CheckInDate) : allBookings.OrderByDescending(x => x.CheckInDate);
                else if (sortBy.Equals("totalprice", StringComparison.OrdinalIgnoreCase))
                    allBookings = isAscending ? allBookings.OrderBy(x => x.TotalPrice) : allBookings.OrderByDescending(x => x.TotalPrice);
            }
            else
            {
                allBookings = allBookings.OrderByDescending(x => x.Id);
            }

            //pagination
            var skipResults = (pageNumber - 1) * pageSize;
            return allBookings.Skip(skipResults).Take(pageSize).Select(ToDto).ToList();
        }

        public BookingWithDetailDTO? GetById(int id)
        {
            return _dbContext.Bookings.Where(b => b.Id == id).Select(ToDto).FirstOrDefault();
        }

        public BookingWithDetailDTO? GetByBookingCode(string bookingCode)
        {
            return _dbContext.Bookings.Where(b => b.BookingCode == bookingCode).Select(ToDto).FirstOrDefault();
        }

        // ================= NHÂN VIÊN ĐẶT PHÒNG TẠI QUẦY =================
        public (bool Success, string Message, BookingWithDetailDTO? Result) Add(AddBookingRequestDTO dto)
        {
            var customer = _dbContext.Customers.FirstOrDefault(c => c.Id == dto.CustomerID);
            if (customer == null)
                return (false, "Không tìm thấy khách hàng", null);

            var checkIn = _policy.ApplyCheckInTime(dto.CheckInDate);
            var checkOut = _policy.ApplyCheckOutTime(dto.CheckOutDate);
            var nights = CountNights(checkIn, checkOut);
            if (nights < 1)
                return (false, "Ngày trả phòng phải sau ngày nhận phòng", null);

            decimal totalPrice = 0;
            foreach (var roomId in dto.RoomIds)
            {
                var room = _dbContext.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.Id == roomId);
                if (room == null)
                    return (false, $"Không tìm thấy phòng id {roomId}", null);
                if (IsRoomBusy(roomId, checkIn, checkOut))
                    return (false, $"Phòng {room.RoomNumber} đã được đặt trong khoảng thời gian này", null);

                totalPrice += room.RoomType.BasePrice * nights;
            }

            // Booking chưa cọc luôn là Pending; trạng thái Confirmed chỉ có sau khi thu cọc
            var bookingDomain = new Booking
            {
                BookingCode = NewBookingCode(),
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Status = "Pending",
                TotalPrice = totalPrice,
                DepositAmount = Math.Round(totalPrice * DepositRate, 0),
                IsDepositPaid = false,
                CreatedDate = DateTime.Now,
                Note = dto.Note,
                CustomerID = dto.CustomerID
            };
            _dbContext.Bookings.Add(bookingDomain);
            _dbContext.SaveChanges();

            foreach (var roomId in dto.RoomIds)
                _dbContext.BookingRooms.Add(new BookingRoom { BookingId = bookingDomain.Id, RoomId = roomId });
            _dbContext.SaveChanges();

            return (true, "Đặt phòng thành công", GetById(bookingDomain.Id));
        }

        public Booking? Delete(int id)
        {
            var domain = _dbContext.Bookings.FirstOrDefault(x => x.Id == id);
            if (domain != null)
            {
                _dbContext.RoomTransfers.RemoveRange(_dbContext.RoomTransfers.Where(t => t.BookingId == id));
                _dbContext.BookingRooms.RemoveRange(_dbContext.BookingRooms.Where(br => br.BookingId == id));
                _dbContext.SaveChanges();

                _dbContext.Bookings.Remove(domain);
                _dbContext.SaveChanges();
            }
            return domain;
        }

        public (bool Success, string Message) UpdateStatus(int id, string status)
        {
            var domain = _dbContext.Bookings.FirstOrDefault(x => x.Id == id);
            if (domain == null) return (false, "Không tìm thấy đặt phòng");

            // Hủy / No-show phải đi qua hàm riêng để tính tiền cọc theo chính sách
            if (status == "Cancelled" || status == "NoShow")
                return (false, "Hủy phòng dùng API staff-cancel, không đến nhận phòng dùng API no-show");
            if (status == "CheckedOut" && domain.Status != "CheckedIn")
                return (false, "Chỉ trả phòng được cho booking đang lưu trú");

            domain.Status = status;
            _dbContext.SaveChanges();
            return (true, "Cập nhật trạng thái thành công");
        }

        // ================= HỦY PHÒNG THEO CHÍNH SÁCH =================
        public (bool Success, string Message, CancellationQuote? Quote) GetCancelQuote(int bookingId, string? userId, bool byHotel = false)
        {
            var booking = _dbContext.Bookings.Include(b => b.Customer).FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng", null);
            if (userId != null && booking.Customer.UserId != userId)
                return (false, "Bạn không có quyền xem đặt phòng này", null);
            return (true, "OK", CancellationPolicy.Quote(booking, DateTime.Now, byHotel));
        }

        public (bool Success, string Message) CancelMyBooking(int bookingId, string userId)
        {
            var booking = _dbContext.Bookings.Include(b => b.Customer).FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
                return (false, "Không tìm thấy đặt phòng");
            if (booking.Customer.UserId != userId)
                return (false, "Bạn không có quyền hủy đặt phòng này");

            return ApplyCancel(booking, byHotel: false, "Khách tự hủy trên website");
        }

        public (bool Success, string Message) StaffCancel(int bookingId, StaffCancelRequestDTO dto)
        {
            var booking = _dbContext.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng");
            var who = dto.ByHotel ? "Khách sạn hủy" : "Lễ tân hủy theo yêu cầu khách";
            return ApplyCancel(booking, dto.ByHotel, string.IsNullOrWhiteSpace(dto.Reason) ? who : $"{who}: {dto.Reason}");
        }

        private (bool Success, string Message) ApplyCancel(Booking booking, bool byHotel, string reason)
        {
            var quote = CancellationPolicy.Quote(booking, DateTime.Now, byHotel);
            if (!quote.CanCancel) return (false, quote.Rule);

            booking.Status = "Cancelled";
            booking.CancelledDate = DateTime.Now;
            booking.CancelReason = $"{reason}. {quote.Rule}";
            booking.CancellationFee = quote.Fee;
            booking.RefundAmount = quote.Refund;
            booking.RefundStatus = quote.Refund > 0 ? "Pending" : null;
            _dbContext.SaveChanges();

            var msg = "Đã hủy đặt phòng. " + quote.Rule;
            if (quote.DepositPaid > 0)
                msg += $" — Hoàn lại {quote.Refund:N0}đ, giữ lại {quote.Fee:N0}đ.";
            return (true, msg);
        }

        // Khách không đến nhận phòng (quá giờ nhận phòng) -> giữ 100% tiền cọc, trả phòng trống cho người khác
        public (bool Success, string Message) MarkNoShow(int bookingId)
        {
            var booking = _dbContext.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng");
            if (booking.Status != "Pending" && booking.Status != "Confirmed")
                return (false, $"Booking đang ở trạng thái '{booking.Status}', không thể đánh dấu không đến");
            if (DateTime.Now < booking.CheckInDate)
                return (false, $"Chưa tới giờ nhận phòng ({booking.CheckInDate:HH:mm dd/MM/yyyy})");

            var paid = booking.IsDepositPaid ? booking.DepositAmount : 0;
            booking.Status = "NoShow";
            booking.CancelledDate = DateTime.Now;
            booking.CancelReason = "Khách không đến nhận phòng (No-show): giữ 100% tiền cọc";
            booking.CancellationFee = paid;
            booking.RefundAmount = 0;
            booking.RefundStatus = null;
            _dbContext.SaveChanges();
            return (true, $"Đã ghi nhận khách không đến. Giữ lại {paid:N0}đ tiền cọc");
        }

        // Khách khai báo tài khoản ngân hàng để nhận tiền hoàn cọc
        public (bool Success, string Message) SetRefundAccount(int bookingId, string userId, RefundAccountDTO dto)
        {
            var booking = _dbContext.Bookings.Include(b => b.Customer).FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng");
            if (booking.Customer.UserId != userId) return (false, "Bạn không có quyền với đặt phòng này");
            if (booking.RefundStatus != "Pending") return (false, "Booking này không có khoản chờ hoàn");

            booking.RefundBankCode = dto.BankCode.Trim().ToUpper();
            booking.RefundAccountNumber = dto.AccountNumber.Trim();
            booking.RefundAccountName = ToBankName(dto.AccountName);
            _dbContext.SaveChanges();
            return (true, "Đã lưu tài khoản nhận tiền hoàn. Khách sạn sẽ chuyển khoản trong 3–7 ngày làm việc");
        }

        // Nhân viên xác nhận đã chuyển trả tiền cọc cho khách -> ghi 1 Payment âm để sổ thu chi khớp
        public (bool Success, string Message) MarkRefunded(int bookingId, MarkRefundedDTO dto)
        {
            var booking = _dbContext.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng");
            if (booking.RefundStatus != "Pending") return (false, "Booking này không có khoản cần hoàn");

            var method = dto.Method == "Cash" ? "tiền mặt tại quầy" : "chuyển khoản";
            booking.RefundStatus = "Done";
            booking.RefundedDate = DateTime.Now;
            booking.RefundNote = $"Hoàn {method}" + (string.IsNullOrWhiteSpace(dto.Note) ? "" : $" — {dto.Note.Trim()}");

            _dbContext.Payments.Add(new Payment
            {
                TransactionCode = "REFUND-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                BookingId = booking.Id,
                Amount = -booking.RefundAmount,
                Status = "Success",
                Method = "Refund",
                CreatedDate = DateTime.Now,
                CompletedDate = DateTime.Now
            });
            _dbContext.SaveChanges();
            return (true, $"Đã ghi nhận hoàn {booking.RefundAmount:N0}đ cho khách ({method})");
        }

        // ================= ĐỔI GIỜ NHẬN / TRẢ PHÒNG =================
        public (bool Success, string Message) ChangeTime(int bookingId, ChangeBookingTimeDTO dto)
        {
            var booking = _dbContext.Bookings
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room).ThenInclude(r => r.RoomType)
                .FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng");
            if (booking.Status != "Pending" && booking.Status != "Confirmed" && booking.Status != "CheckedIn")
                return (false, $"Booking đang ở trạng thái '{booking.Status}', không thể đổi giờ");

            // Bỏ phần giây cho gọn
            var newIn = dto.CheckInDate.AddSeconds(-dto.CheckInDate.Second).AddMilliseconds(-dto.CheckInDate.Millisecond);
            var newOut = dto.CheckOutDate.AddSeconds(-dto.CheckOutDate.Second).AddMilliseconds(-dto.CheckOutDate.Millisecond);

            if (booking.Status == "CheckedIn" && Math.Abs((newIn - booking.CheckInDate).TotalMinutes) >= 1)
                return (false, "Khách đã nhận phòng, chỉ đổi được giờ trả phòng");
            if (newOut <= newIn)
                return (false, "Giờ trả phòng phải sau giờ nhận phòng");
            var newNights = CountNights(newIn, newOut);
            if (newNights < 0)
                return (false, "Thời gian lưu trú không hợp lệ");

            foreach (var br in booking.BookingRooms)
            {
                if (IsRoomBusy(br.RoomId, newIn, newOut, booking.Id))
                    return (false, $"Phòng {br.Room.RoomNumber} đã có khách khác đặt trong khung giờ mới");
            }

            // Số đêm thay đổi (gia hạn / về sớm) -> cộng/trừ tiền phòng theo giá phòng hiện tại
            var oldNights = CountNights(booking.CheckInDate, booking.CheckOutDate);
            var nightDiff = newNights - oldNights;
            var pricePerNight = booking.BookingRooms.Sum(br => br.Room.RoomType.BasePrice);
            if (nightDiff != 0)
            {
                booking.TotalPrice = Math.Max(0, booking.TotalPrice + pricePerNight * nightDiff);
                if (!booking.IsDepositPaid)
                    booking.DepositAmount = Math.Round(booking.TotalPrice * DepositRate, 0);
            }

            booking.CheckInDate = newIn;
            booking.CheckOutDate = newOut;
            _dbContext.SaveChanges();

            var msg = $"Đã đổi lịch: nhận {newIn:HH:mm dd/MM/yyyy} — trả {newOut:HH:mm dd/MM/yyyy}";
            if (nightDiff > 0) msg += $". Gia hạn thêm {nightDiff} đêm (+{pricePerNight * nightDiff:N0}đ)";
            if (nightDiff < 0) msg += $". Rút ngắn {-nightDiff} đêm ({pricePerNight * nightDiff:N0}đ)";
            return (true, msg);
        }

        // Lễ tân thu cọc tiền mặt / MoMo tại quầy -> ghi nhận 1 Payment "CASH-..." và xác nhận booking
        public (bool Success, string Message) ConfirmDepositAtDesk(int bookingId)
        {
            var booking = _dbContext.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng");
            if (booking.Status != "Pending" && booking.Status != "Confirmed")
                return (false, $"Booking đang ở trạng thái '{booking.Status}', không thể thu cọc");
            if (booking.IsDepositPaid) return (false, "Booking này đã thanh toán cọc");

            _dbContext.Payments.Add(new Payment
            {
                TransactionCode = "CASH-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                BookingId = booking.Id,
                Amount = booking.DepositAmount,
                Status = "Success",
                Method = "Cash",
                CreatedDate = DateTime.Now,
                CompletedDate = DateTime.Now
            });
            booking.IsDepositPaid = true;
            if (booking.Status == "Pending") booking.Status = "Confirmed";
            _dbContext.SaveChanges();
            return (true, "Đã thu tiền cọc tại quầy, booking được xác nhận");
        }

        // Tên chủ tài khoản theo chuẩn ngân hàng: IN HOA, không dấu ("Đặng Văn Á" -> "DANG VAN A")
        private static string ToBankName(string name)
        {
            var normalized = name.Trim().Replace("đ", "d").Replace("Đ", "D").Normalize(System.Text.NormalizationForm.FormD);
            var chars = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
            return System.Text.RegularExpressions.Regex.Replace(new string(chars.ToArray()), @"\s+", " ").ToUpper();
        }

        private static string NewBookingCode() => "BK-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
    }
}
