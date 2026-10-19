using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    // Trang Dịch vụ công khai cho khách (khác ServiceManage là trang quản trị)
    public class ServicesController : BaseApiController
    {
        public ServicesController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var services = await GetListAsync<ServiceDTO>("/api/Services/get-all-services");
            var rooms = await GetListAsync<RoomRawDTO>("/api/Rooms/get-all-rooms?pageSize=1000");
            var imgs = rooms.Where(r => !string.IsNullOrEmpty(r.ImageUrl)).Select(r => r.ImageUrl).ToList();
            ViewBag.HeroImage = imgs.Skip(2).FirstOrDefault() ?? imgs.FirstOrDefault();
            return View(services);
        }
    }
}
