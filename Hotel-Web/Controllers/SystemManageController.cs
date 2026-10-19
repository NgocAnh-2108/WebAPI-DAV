using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    // Hệ thống — chỉ Manager
    public class SystemManageController : BaseApiController
    {
        public SystemManageController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        [HttpGet]
        public async Task<IActionResult> Accounts(string? role)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            ViewBag.PageTitle = "Tài khoản & Phân quyền";
            ViewBag.FilterRole = role;
            var accounts = await GetListAsync<StaffAccountDTO>("/api/User/get-all-accounts");
            ViewBag.Counts = accounts.GroupBy(a => a.RoleName).ToDictionary(g => g.Key, g => g.Count());
            if (!string.IsNullOrEmpty(role)) accounts = accounts.Where(a => a.RoleName == role).ToList();
            return View(accounts);
        }

        [HttpPost]
        public async Task<IActionResult> AddStaff(RegisterStaffDTO dto, string? returnTo)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/User/Register", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = response.IsSuccessStatusCode
                ? $"Đã tạo tài khoản {dto.Username}"
                : "Không tạo được tài khoản (email đã tồn tại hoặc mật khẩu < 6 ký tự)";
            return RedirectToAction(returnTo == "Staff" ? "Staff" : "Accounts");
        }

        [HttpGet]
        public async Task<IActionResult> Staff()
        {
            var denied = CheckManager(); if (denied != null) return denied;
            ViewBag.PageTitle = "Quản lý nhân viên";
            var accounts = await GetListAsync<StaffAccountDTO>("/api/User/get-all-accounts");
            return View(accounts.Where(a => a.RoleName == "Manager" || a.RoleName == "Receptionist").ToList());
        }

        // Quản lý xóa tài khoản cấp dưới (Lễ tân, Khách hàng) — API tự kiểm tra phân cấp
        [HttpPost]
        public async Task<IActionResult> DeleteAccount(string id, string? returnTo)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/User/delete-account/{id}");
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return RedirectToAction(returnTo == "Staff" ? "Staff" : "Accounts");
        }
    }
}
