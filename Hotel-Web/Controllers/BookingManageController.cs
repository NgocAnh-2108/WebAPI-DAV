using Hotel_Web.Helpers;
using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    // Quản lý đặt phòng — dành cho Manager và Receptionist (Lễ tân)
    public class BookingManageController : BaseApiController
    {
        public BookingManageController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        // ================= DANH SÁCH BOOKING =================
        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? q)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Danh sách Booking";
            ViewBag.FilterStatus = status;
            ViewBag.Query = q;
            ViewBag.IsManager = IsManager();

            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            ViewBag.RefundCount = bookings.Count(b => b.RefundStatus == "Pending");
            if (status == "Refund") bookings = bookings.Where(b => b.RefundStatus == "Pending").ToList();
            else if (!string.IsNullOrEmpty(status)) bookings = bookings.Where(b => b.Status == status).ToList();

            if (!string.IsNullOrWhiteSpace(q))
                bookings = bookings.Where(b => (b.BookingCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (b.CustomerName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (b.CustomerPhone ?? "").Contains(q)).ToList();

            return View(bookings.OrderByDescending(b => b.CreatedDate).ToList());
        }

        // Lễ tân thu tiền cọc trực tiếp tại quầy
        [HttpPost]
        public async Task<IActionResult> ConfirmDeposit(int id, string? returnUrl)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsync($"{ApiBaseUrl}/api/Bookings/confirm-deposit/{id}", null);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Đã thu tiền cọc, booking chuyển sang Đã xác nhận" : await ReadError(response);
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/BookingManage/Index" : returnUrl);
        }

        // ================= HỦY BOOKING THEO CHÍNH SÁCH =================
        // Trang hủy: so sánh 2 cách hủy — theo yêu cầu khách (áp chính sách) / khách sạn hủy (hoàn 100%)
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var booking = await GetOneAsync<BookingWithDetailDTO>($"/api/Bookings/get-booking-by-id/{id}");
            if (booking == null) { TempData["Error"] = "Không tìm thấy booking"; return RedirectToAction("Index"); }
            ViewBag.PageTitle = "Hủy booking";
            ViewBag.GuestQuote = await GetOneAsync<CancellationQuoteDTO>($"/api/Bookings/cancel-preview/{id}");
            ViewBag.HotelQuote = await GetOneAsync<CancellationQuoteDTO>($"/api/Bookings/cancel-preview/{id}?byHotel=true");
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id, bool byHotel, string? reason)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Bookings/staff-cancel/{id}",
                new StaffCancelDTO { ByHotel = byHotel, Reason = reason });
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return RedirectToAction("Index");
        }

        // Khách không đến nhận phòng -> giữ 100% tiền cọc
        [HttpPost]
        public async Task<IActionResult> NoShow(int id)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsync($"{ApiBaseUrl}/api/Bookings/no-show/{id}", null);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return RedirectToAction("Index");
        }


        // ================= ĐỔI GIỜ NHẬN / TRẢ PHÒNG =================
        [HttpGet]
        public async Task<IActionResult> ChangeTime(int id)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var booking = await GetOneAsync<BookingWithDetailDTO>($"/api/Bookings/get-booking-by-id/{id}");
            if (booking == null) { TempData["Error"] = "Không tìm thấy booking"; return RedirectToAction("Index"); }
            ViewBag.PageTitle = "Đổi giờ nhận / trả phòng";
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeTime(int id, DateTime checkInDate, DateTime checkOutDate, string? returnUrl)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Bookings/change-time/{id}",
                new ChangeBookingTimeDTO { CheckInDate = checkInDate, CheckOutDate = checkOutDate });
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            if (!response.IsSuccessStatusCode) return RedirectToAction("ChangeTime", new { id });
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/BookingManage/Index" : returnUrl);
        }

        // ================= CHUYỂN PHÒNG =================
        [HttpGet]
        public async Task<IActionResult> Transfer(int id, int? fromRoomId)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Chuyển phòng";
            var resp = await GetClient().GetAsync($"{ApiBaseUrl}/api/Bookings/transfer-options/{id}?fromRoomId={fromRoomId}");
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = await ReadError(resp);
                return RedirectToAction("Index");
            }
            ViewBag.History = await GetListAsync<RoomTransferDTO>($"/api/Bookings/transfers/{id}");
            return View(await resp.Content.ReadFromJsonAsync<TransferOptionsResultDTO>());
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(int id, TransferRoomRequestDTO dto)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/Bookings/transfer-room/{id}", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return response.IsSuccessStatusCode
                ? RedirectToAction("CheckOut")
                : RedirectToAction("Transfer", new { id, fromRoomId = dto.FromRoomId });
        }

        // Xóa hẳn booking — chỉ Manager
        [HttpPost]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/Bookings/delete-booking-by-id/{id}");
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Đã xóa booking" : await ReadError(response);
            return RedirectToAction("Index");
        }

        // ================= LỊCH ĐẶT PHÒNG (timeline 14 ngày) =================
        [HttpGet]
        public async Task<IActionResult> Calendar(DateTime? from)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Lịch đặt phòng";

            var start = (from ?? DateTime.Today).Date;
            ViewBag.From = start;
            ViewBag.Days = Enumerable.Range(0, 14).Select(i => start.AddDays(i)).ToList();
            ViewBag.Bookings = (await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000"))
                .Where(b => b.Status != "Cancelled" && b.Status != "NoShow" && b.CheckOutDate.Date > start && b.CheckInDate.Date < start.AddDays(14))
                .ToList();
            var rooms = await GetListAsync<ManageRoomDTO>("/api/Rooms/get-all-rooms?pageSize=1000");
            return View(rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToList());
        }

        // ================= TẠO BOOKING TẠI QUẦY (khách vãng lai / gọi điện) =================
        [HttpGet]
        public async Task<IActionResult> NewBooking(DateTime? checkIn, DateTime? checkOut)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Tạo booking tại quầy";
            ViewBag.Customers = await GetListAsync<CustomerDTO>("/api/Customers/get-all-customers");

            var ci = checkIn ?? DateTime.Today;
            var co = checkOut ?? ci.AddDays(1);
            ViewBag.CheckIn = ci.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = co.ToString("yyyy-MM-dd");

            var rooms = new List<RoomAvailableDTO>();
            if (co > ci)
                rooms = await GetListAsync<RoomAvailableDTO>($"/api/Rooms/available?checkIn={ci:yyyy-MM-dd}&checkOut={co:yyyy-MM-dd}");
            else
                ViewBag.Error = "Ngày trả phòng phải sau ngày nhận phòng";
            return View(rooms);
        }

        [HttpPost]
        public async Task<IActionResult> NewBooking(DateTime checkIn, DateTime checkOut, int? customerId,
            string? newFullName, string? newPhone, string? newIdentityCard, List<int> roomIds, string? note, bool collectDeposit)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var back = RedirectToAction("NewBooking", new { checkIn = checkIn.ToString("yyyy-MM-dd"), checkOut = checkOut.ToString("yyyy-MM-dd") });
            var client = GetClient();

            if (roomIds == null || roomIds.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất 1 phòng";
                return back;
            }

            // 1) Khách mới -> tạo hồ sơ khách hàng trước
            if (customerId is null or 0)
            {
                if (string.IsNullOrWhiteSpace(newFullName) || string.IsNullOrWhiteSpace(newPhone))
                {
                    TempData["Error"] = "Chọn khách hàng có sẵn hoặc nhập Họ tên + SĐT khách mới";
                    return back;
                }
                var addResp = await client.PostAsJsonAsync($"{ApiBaseUrl}/api/Customers/add-customer",
                    new AddCustomerDTO { FullName = newFullName, Phone = newPhone, IdentityCard = newIdentityCard });
                if (!addResp.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không tạo được khách hàng: " + await ReadError(addResp);
                    return back;
                }
                var customers = await GetListAsync<CustomerDTO>("/api/Customers/get-all-customers");
                customerId = customers.Where(c => c.Phone == newPhone).OrderByDescending(c => c.Id).FirstOrDefault()?.Id;
            }

            // 2) Tạo booking
            var dto = new StaffBookingRequestDTO
            {
                CheckInDate = checkIn, CheckOutDate = checkOut, CustomerID = customerId ?? 0,
                RoomIds = roomIds, Note = note, Status = "Pending"
            };
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/api/Bookings/add-booking", dto);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Đặt phòng thất bại: " + await ReadError(response);
                return back;
            }

            // 3) API trả về booking vừa tạo (có mã) -> (tuỳ chọn) thu cọc luôn
            BookingWithDetailDTO? created = null;
            try { created = await response.Content.ReadFromJsonAsync<BookingWithDetailDTO>(); } catch { }

            if (created != null && collectDeposit)
                await client.PutAsync($"{ApiBaseUrl}/api/Bookings/confirm-deposit/{created.Id}", null);

            TempData["Message"] = created != null
                ? $"Tạo booking thành công — Mã: {created.BookingCode}" + (collectDeposit ? " (đã thu cọc)" : "")
                : "Tạo booking thành công";
            return RedirectToAction("Index");
        }

        // ================= NHẬN PHÒNG =================
        [HttpGet]
        public async Task<IActionResult> CheckIn(string? code)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Nhận phòng";

            var all = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            ViewBag.Arrivals = all.Where(b => b.CheckInDate.Date <= DateTime.Today && b.CheckOutDate.Date > DateTime.Today
                    && (b.Status == "Confirmed" || b.Status == "Pending"))
                .OrderBy(b => b.CheckInDate).ToList();

            if (!string.IsNullOrWhiteSpace(code))
            {
                var resp = await GetClient().GetAsync($"{ApiBaseUrl}/api/Bookings/by-code/{code.Trim()}");
                if (resp.IsSuccessStatusCode)
                    ViewBag.Booking = await resp.Content.ReadFromJsonAsync<BookingWithDetailDTO>();
                else
                    ViewBag.LookupError = $"Không tìm thấy mã booking \"{code}\"";
                ViewBag.Code = code;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCheckIn(string bookingCode)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsync($"{ApiBaseUrl}/api/Bookings/checkin-by-code/{bookingCode}", null);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? $"Check-in thành công cho mã {bookingCode}" : await ReadError(response);
            return RedirectToAction("CheckIn");
        }

        // ================= TRẢ PHÒNG =================
        [HttpGet]
        public async Task<IActionResult> CheckOut()
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Trả phòng";
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?filterOn=status&filterQuery=CheckedIn&pageSize=5000");
            var usages = await GetListAsync<BookingServiceDTO>("/api/Services/usages");
            ViewBag.ServiceTotals = usages.GroupBy(u => u.BookingId).ToDictionary(g => g.Key, g => g.Sum(u => u.Total));
            return View(bookings.OrderBy(b => b.CheckOutDate).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCheckOut(int bookingId)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Bookings/update-status/{bookingId}", "CheckedOut");
            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Trả phòng thành công — có thể in hóa đơn cho khách";
                return RedirectToAction("InvoiceDetail", "FinanceManage", new { id = bookingId });
            }
            TempData["Error"] = await ReadError(response);
            return RedirectToAction("CheckOut");
        }
    }
}
