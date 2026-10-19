using Hotel_Web.Helpers;
using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiBaseUrl = "https://localhost:7167";

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO dto, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            dto.Roles = new[] { "Customer" }; 
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/api/User/Register", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Đăng ký thành công! Mời quý khách đăng nhập.";
                return RedirectToAction("Login", new { returnUrl });
            }

            ViewBag.Error = "Không thể đăng ký: email đã được sử dụng hoặc mật khẩu dưới 6 ký tự.";
            return View(dto);
        }

        // returnUrl = trang khách đang xem trước khi bị yêu cầu đăng nhập (VD: trang đặt phòng)
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO dto, string? returnUrl)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/api/User/Login", dto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
                HttpContext.Session.SetString("JwtToken", result!.JwtToken);
                HttpContext.Session.SetString("Username", dto.Username);

                var role = RoleHelper.MainRole(HttpContext.Session);
                HttpContext.Session.SetString("Role", role ?? "");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

                // Nhân viên vào thẳng trang quản trị, khách hàng về trang chủ
                if (role == "Manager" || role == "Receptionist")
                    return RedirectToAction("Dashboard", "Manage");
                return RedirectToAction("Index", "Rooms");
            }

            ViewBag.ReturnUrl = returnUrl;   // giữ lại để đăng nhập lại lần sau vẫn quay về đúng trang
            ViewBag.Error = "Email hoặc mật khẩu không đúng";
            return View(dto);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken"))) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp";
                return View();
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/api/User/change-password",
                new ChangePasswordDTO { CurrentPassword = currentPassword, NewPassword = newPassword });

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Đổi mật khẩu thành công";
                return RoleHelper.IsStaff(HttpContext.Session) ? RedirectToAction("Dashboard", "Manage") : RedirectToAction("Index", "Rooms");
            }
            ViewBag.Error = "Mật khẩu hiện tại không đúng hoặc mật khẩu mới chưa hợp lệ (tối thiểu 6 ký tự)";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
