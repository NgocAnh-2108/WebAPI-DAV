using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    public class CustomerManageController : BaseApiController
    {
        public CustomerManageController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Danh sách khách hàng";
            ViewBag.Query = q;
            ViewBag.IsManager = IsManager();

            var customers = await GetListAsync<CustomerDTO>("/api/Customers/get-all-customers");
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            ViewBag.BookingCount = bookings.GroupBy(b => b.CustomerPhone ?? "").ToDictionary(g => g.Key, g => g.Count());

            if (!string.IsNullOrWhiteSpace(q))
                customers = customers.Where(c => (c.FullName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (c.Phone ?? "").Contains(q) || (c.Email ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (c.IdentityCard ?? "").Contains(q)).ToList();
            return View(customers.OrderByDescending(c => c.Id).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var resp = await GetClient().GetAsync($"{ApiBaseUrl}/api/Customers/get-customer-by-id/{id}");
            if (!resp.IsSuccessStatusCode) { TempData["Error"] = "Không tìm thấy khách hàng"; return RedirectToAction("Index"); }
            var customer = await resp.Content.ReadFromJsonAsync<CustomerDTO>();
            ViewBag.PageTitle = "Hồ sơ khách hàng";
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            ViewBag.Bookings = bookings.Where(b => b.CustomerPhone == customer!.Phone && b.CustomerName == customer.FullName)
                .OrderByDescending(b => b.CreatedDate).ToList();
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddCustomerDTO dto)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/Customers/add-customer", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? $"Đã thêm khách hàng {dto.FullName}" : await ReadError(response);
            return RedirectToAction("Index");
        }

        // Lễ tân + Quản lý xóa khách hàng (khách đã có booking thì API từ chối)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/Customers/delete-customer-by-id/{id}");
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, AddCustomerDTO dto)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Customers/update-customer-by-id/{id}", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Đã cập nhật thông tin khách hàng" : await ReadError(response);
            return RedirectToAction("Detail", new { id });
        }
    }
}
