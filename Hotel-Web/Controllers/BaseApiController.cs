using Hotel_Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;

namespace Hotel_Web.Controllers
{
    public class BaseApiController : Controller
    {
        protected const string ApiBaseUrl = "https://localhost:7167";
        protected readonly IHttpClientFactory _httpClientFactory;

        public BaseApiController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token) && RoleHelper.IsExpired(token))
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại";
                context.Result = RedirectToAction("Login", "Account");
                return;
            }
            base.OnActionExecuting(context);
        }

        protected HttpClient GetClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        protected bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken"));

        protected bool IsInRole(string role) => RoleHelper.GetRoles(HttpContext.Session.GetString("JwtToken")).Contains(role);

        protected bool IsManager() => IsInRole("Manager");

        protected bool IsStaff() => IsManager() || IsInRole("Receptionist");

        // Chưa đăng nhập -> sang trang Đăng nhập, kèm returnUrl để đăng nhập xong quay lại đúng trang đang xem
        protected IActionResult RedirectToLogin()
        {
            var back = Request.Path + Request.QueryString;
            // Đang gửi form (POST) thì quay về trang chứa form đó
            if (!HttpMethods.IsGet(Request.Method))
            {
                var referer = Request.Headers.Referer.ToString();
                back = Uri.TryCreate(referer, UriKind.Absolute, out var u) ? u.PathAndQuery : "/";
            }
            return RedirectToAction("Login", "Account", new { returnUrl = back });
        }

        // Chỉ Manager
        protected IActionResult? CheckManager()
        {
            if (!IsLoggedIn()) return RedirectToLogin();
            if (!IsManager())
            {
                TempData["Error"] = "Chức năng này chỉ dành cho Quản lý";
                return IsStaff() ? RedirectToAction("Dashboard", "Manage") : RedirectToAction("Index", "Rooms");
            }
            return null;
        }

        // Manager hoặc Receptionist
        protected IActionResult? CheckStaff()
        {
            if (!IsLoggedIn()) return RedirectToLogin();
            if (!IsStaff())
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang quản trị";
                return RedirectToAction("Index", "Rooms");
            }
            return null;
        }

        // Đọc message lỗi từ API ({"message": "..."} hoặc text thường)
        protected static async Task<string> ReadError(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var p in doc.RootElement.EnumerateObject())
                        if (p.Name.Equals("message", StringComparison.OrdinalIgnoreCase)) return p.Value.ToString();
                    if (doc.RootElement.TryGetProperty("errors", out var errs)) return errs.ToString();
                }
            }
            catch { }
            if ((int)response.StatusCode == 401 || (int)response.StatusCode == 403) return "Bạn không có quyền thực hiện thao tác này";
            return string.IsNullOrWhiteSpace(text) ? $"Lỗi {(int)response.StatusCode}" : text.Trim('"');
        }

        // Gọi API trả về 1 đối tượng (null nếu lỗi)
        protected async Task<T?> GetOneAsync<T>(string url) where T : class
        {
            try
            {
                var resp = await GetClient().GetAsync(ApiBaseUrl + url);
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadFromJsonAsync<T>();
            }
            catch { return null; }
        }

        protected async Task<List<T>> GetListAsync<T>(string url)
        {
            try
            {
                var resp = await GetClient().GetAsync(ApiBaseUrl + url);
                if (!resp.IsSuccessStatusCode) return new List<T>();
                return await resp.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
            }
            catch { return new List<T>(); }
        }
    }
}
