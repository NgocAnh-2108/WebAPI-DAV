using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    // DỊCH VỤ — Manager: thêm/sửa/xóa dịch vụ | Manager + Lễ tân: ghi nhận dịch vụ khách sử dụng
    public class ServiceManageController : BaseApiController
    {
        public ServiceManageController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        public static readonly string[] Categories = { "Ẩm thực", "Spa & Thư giãn", "Giặt ủi", "Di chuyển", "Khác" };

        // ================= DANH SÁCH DỊCH VỤ =================
        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Danh sách dịch vụ";
            ViewBag.IsManager = IsManager();
            ViewBag.Category = category;
            var services = await GetListAsync<ServiceDTO>("/api/Services/get-all-services?includeInactive=true");
            ViewBag.CountByCat = services.GroupBy(s => s.Category).ToDictionary(g => g.Key, g => g.Count());
            if (!string.IsNullOrEmpty(category)) services = services.Where(s => s.Category == category).ToList();
            return View(services);
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddServiceDTO dto)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/Services/add-service", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? $"Đã thêm dịch vụ {dto.Name}" : await ReadError(response);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, AddServiceDTO dto)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Services/update-service-by-id/{id}", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? $"Đã cập nhật dịch vụ {dto.Name}" : await ReadError(response);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/Services/delete-service-by-id/{id}");
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return RedirectToAction("Index");
        }

        // ================= DỊCH VỤ KHÁCH SỬ DỤNG =================
        [HttpGet]
        public async Task<IActionResult> Usage(int? bookingId, DateTime? from, DateTime? to)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Dịch vụ khách sử dụng";

            // Booking được phép ghi dịch vụ: đã xác nhận hoặc đang lưu trú
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            ViewBag.ActiveBookings = bookings.Where(b => b.Status == "CheckedIn" || b.Status == "Confirmed")
                .OrderByDescending(b => b.Status == "CheckedIn").ThenBy(b => b.CheckInDate).ToList();
            ViewBag.Services = await GetListAsync<ServiceDTO>("/api/Services/get-all-services");
            ViewBag.BookingId = bookingId;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            var url = "/api/Services/usages?";
            if (bookingId.HasValue) url += $"bookingId={bookingId}&";
            if (from.HasValue) url += $"from={from:yyyy-MM-dd}&";
            if (to.HasValue) url += $"to={to:yyyy-MM-dd}&";
            return View(await GetListAsync<BookingServiceDTO>(url));
        }

        [HttpPost]
        public async Task<IActionResult> AddUsage(AddBookingServiceDTO dto, string? returnUrl)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/Services/add-usage", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/ServiceManage/Usage" : returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUsage(int id, string? returnUrl)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/Services/delete-usage/{id}");
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/ServiceManage/Usage" : returnUrl);
        }
    }
}
