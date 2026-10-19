using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<BookingsController> _logger;
        private readonly ICustomerRepository _customerRepository;
        private readonly IRoomTransferRepository _roomTransferRepository;

        public BookingsController(IBookingRepository bookingRepository, ILogger<BookingsController> logger,
            ICustomerRepository customerRepository, IRoomTransferRepository roomTransferRepository)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
            _customerRepository = customerRepository;
            _roomTransferRepository = roomTransferRepository;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ================= KHÁCH HÀNG =================

        // POST /api/Bookings/book-room — khách tự đặt phòng (giờ nhận 14:00 / trả 12:00 tự gắn theo HotelPolicy)
        [HttpPost("book-room")]
        [Authorize(Roles = "Customer")]
        public IActionResult BookRoom([FromBody] CustomerBookingRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (success, message, result) = _bookingRepository.AddForCustomer(dto, CurrentUserId!, _customerRepository);
            if (!success) return BadRequest(new { message });
            return Ok(new { message, booking = result });
        }

        // GET /api/Bookings/my-bookings — khách xem booking của mình
        [HttpGet("my-bookings")]
        [Authorize(Roles = "Customer")]
        public IActionResult GetMyBookings()
        {
            return Ok(_bookingRepository.GetMyBookings(CurrentUserId!));
        }

        // GET /api/Bookings/cancel-preview/5 — xem trước nếu hủy bây giờ thì được hoàn bao nhiêu
        [HttpGet("cancel-preview/{id}")]
        [Authorize(Roles = "Customer,Receptionist,Manager")]
        public IActionResult CancelPreview(int id, [FromQuery] bool byHotel = false)
        {
            var userId = User.IsInRole("Customer") ? CurrentUserId : null;
            var (success, message, quote) = _bookingRepository.GetCancelQuote(id, userId, byHotel && userId == null);
            if (!success) return BadRequest(new { message });
            return Ok(quote);
        }

        // DELETE /api/Bookings/cancel/5 — khách tự hủy, tiền cọc hoàn/giữ theo chính sách hủy
        [HttpDelete("cancel/{id}")]
        [Authorize(Roles = "Customer")]
        public IActionResult CancelMyBooking(int id)
        {
            var (success, message) = _bookingRepository.CancelMyBooking(id, CurrentUserId!);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // PUT /api/Bookings/refund-account/5 — khách khai báo tài khoản nhận tiền hoàn cọc
        [HttpPut("refund-account/{id}")]
        [Authorize(Roles = "Customer")]
        public IActionResult SetRefundAccount(int id, [FromBody] RefundAccountDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (success, message) = _bookingRepository.SetRefundAccount(id, CurrentUserId!, dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // ================= NHÂN VIÊN =================

        // GET: /api/Bookings/get-all-bookings?filterOn=status&filterQuery=Pending&sortBy=checkindate&isAscending=false
        [HttpGet("get-all-bookings")]
        public IActionResult GetAll([FromQuery] string? filterOn, [FromQuery] string? filterQuery,
            [FromQuery] string? sortBy, [FromQuery] bool isAscending = true,
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            return Ok(_bookingRepository.GetAll(filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize));
        }

        [HttpGet("get-booking-by-id/{id}")]
        public IActionResult GetById(int id)
        {
            var result = _bookingRepository.GetById(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy đặt phòng" });
            return Ok(result);
        }

        [HttpPost("add-booking")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult Add([FromBody] AddBookingRequestDTO dto)
        {
            _logger.LogInformation($"AddBooking invoked with data {JsonSerializer.Serialize(dto)}");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message, result) = _bookingRepository.Add(dto);
            if (!success)
            {
                _logger.LogWarning($"AddBooking failed: {message}");
                return BadRequest(new { message });
            }
            _logger.LogInformation("AddBooking succeeded");
            return Ok(result);
        }

        // PUT /api/Bookings/update-status/5 — dùng cho trả phòng ("CheckedOut")
        [HttpPut("update-status/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult UpdateStatus(int id, [FromBody] string status)
        {
            var (success, message) = _bookingRepository.UpdateStatus(id, status);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        [HttpDelete("delete-booking-by-id/{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult Delete(int id)
        {
            var result = _bookingRepository.Delete(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy đặt phòng" });
            return Ok(new { message = "Đã xóa booking", result.Id });
        }

        // GET /api/Bookings/by-code/BK-A1B2C3D4
        [HttpGet("by-code/{bookingCode}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult GetByBookingCode(string bookingCode)
        {
            var result = _bookingRepository.GetByBookingCode(bookingCode);
            if (result == null) return NotFound(new { message = "Không tìm thấy mã đặt phòng này" });
            return Ok(result);
        }

        // PUT /api/Bookings/checkin-by-code/BK-A1B2C3D4 — lễ tân xác nhận check-in nhanh
        [HttpPut("checkin-by-code/{bookingCode}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult CheckInByCode(string bookingCode)
        {
            var booking = _bookingRepository.GetByBookingCode(bookingCode);
            if (booking == null) return NotFound(new { message = "Không tìm thấy mã đặt phòng này" });

            if (!booking.IsDepositPaid)
                return BadRequest(new { message = "Khách chưa thanh toán tiền cọc, không thể nhận phòng" });

            if (booking.Status != "Confirmed")
                return BadRequest(new { message = $"Trạng thái hiện tại là '{booking.Status}', không thể check-in" });

            var (success, message) = _bookingRepository.UpdateStatus(booking.Id, "CheckedIn");
            if (!success) return BadRequest(new { message });
            return Ok(new { message = "Check-in thành công", bookingCode });
        }

        // PUT /api/Bookings/confirm-deposit/5 — lễ tân thu tiền cọc tại quầy (tiền mặt / đối chiếu MoMo)
        [HttpPut("confirm-deposit/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult ConfirmDepositAtDesk(int id)
        {
            var (success, message) = _bookingRepository.ConfirmDepositAtDesk(id);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // PUT /api/Bookings/change-time/5 — đổi giờ nhận / giờ trả phòng cho khách
        [HttpPut("change-time/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult ChangeTime(int id, [FromBody] ChangeBookingTimeDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (success, message) = _bookingRepository.ChangeTime(id, dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // PUT /api/Bookings/staff-cancel/5 — nhân viên hủy: theo yêu cầu khách (áp chính sách) hoặc khách sạn hủy (hoàn 100%)
        [HttpPut("staff-cancel/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult StaffCancel(int id, [FromBody] StaffCancelRequestDTO dto)
        {
            var (success, message) = _bookingRepository.StaffCancel(id, dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // PUT /api/Bookings/no-show/5 — khách không đến nhận phòng: giữ 100% tiền cọc
        [HttpPut("no-show/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult MarkNoShow(int id)
        {
            var (success, message) = _bookingRepository.MarkNoShow(id);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // PUT /api/Bookings/mark-refunded/5 — lễ tân / quản lý xác nhận đã hoàn tiền cọc cho khách
        [HttpPut("mark-refunded/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult MarkRefunded(int id, [FromBody] MarkRefundedDTO dto)
        {
            var (success, message) = _bookingRepository.MarkRefunded(id, dto ?? new MarkRefundedDTO());
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // ================= CHUYỂN PHÒNG =================

        // GET /api/Bookings/transfer-options/5?fromRoomId=3 — danh sách phòng có thể chuyển sang
        [HttpGet("transfer-options/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult TransferOptions(int id, [FromQuery] int? fromRoomId)
        {
            var (success, message, result) = _roomTransferRepository.GetOptions(id, fromRoomId);
            if (!success) return BadRequest(new { message });
            return Ok(result);
        }

        // POST /api/Bookings/transfer-room/5 — chuyển khách sang phòng khác
        [HttpPost("transfer-room/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult TransferRoom(int id, [FromBody] TransferRoomRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (success, message) = _roomTransferRepository.Transfer(id, dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // GET /api/Bookings/transfers/5 — lịch sử chuyển phòng của booking
        [HttpGet("transfers/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult Transfers(int id)
        {
            return Ok(_roomTransferRepository.GetByBooking(id));
        }
    }
}
