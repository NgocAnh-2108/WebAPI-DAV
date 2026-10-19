using Hotel_Web.Helpers;
using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    public class RoomsController : BaseApiController
    {
        public RoomsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        private void SetUserFlags()
        {
            ViewBag.LoggedIn = IsLoggedIn();
            ViewBag.Role = RoleHelper.MainRole(HttpContext.Session);
        }

        private async Task<List<RoomAvailableDTO>> LoadAllRooms()
        {
            var types = await GetListAsync<RoomTypeDTO>("/api/RoomTypes/get-all-roomtypes");
            var raw = await GetListAsync<RoomRawDTO>("/api/Rooms/get-all-rooms?pageSize=1000&sortBy=price");
            return raw.Select(r =>
            {
                var t = types.FirstOrDefault(x => x.Name == r.RoomTypeName);
                return new RoomAvailableDTO
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    RoomTypeName = r.RoomTypeName,
                    BasePrice = r.BasePrice,
                    MaxOccupancy = t?.MaxOccupancy ?? 2,
                    Description = t?.Description,
                    Floor = r.Floor,
                    ImageUrls = string.IsNullOrEmpty(r.ImageUrl) ? new() : new() { r.ImageUrl },
                    Status = r.Status
                };
            }).ToList();
        }

        // Trang chủ: giới thiệu khách sạn, phòng nổi bật, tiện ích
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SetUserFlags();
            var rooms = await LoadAllRooms();
            ViewBag.TotalRooms = rooms.Count;
            ViewBag.TypeCount = rooms.Select(r => r.RoomTypeName).Distinct().Count();
            ViewBag.HeroImage = rooms.SelectMany(r => r.ImageUrls).FirstOrDefault();
            ViewBag.Services = await GetListAsync<ServiceDTO>("/api/Services/get-all-services");

            // Phòng nổi bật: mỗi hạng phòng 1 phòng (ưu tiên phòng có ảnh), hạng cao giá trước, tối đa 3
            var featured = rooms.Where(r => r.Status != "Maintenance")
                .GroupBy(r => r.RoomTypeName)
                .Select(g => g.OrderByDescending(r => r.ImageUrls.Any()).First())
                .OrderByDescending(r => r.BasePrice)
                .Take(3).ToList();
            if (featured.Count < 3)
                featured.AddRange(rooms.Where(r => r.Status != "Maintenance" && !featured.Contains(r))
                    .OrderByDescending(r => r.ImageUrls.Any()).ThenByDescending(r => r.BasePrice).Take(3 - featured.Count));
            return View(featured);
        }

        // Trang Phòng nghỉ: tất cả phòng, lọc theo hạng phòng
        [HttpGet]
        public async Task<IActionResult> List(string? type)
        {
            SetUserFlags();
            var rooms = await LoadAllRooms();
            ViewBag.Types = rooms.Select(r => r.RoomTypeName).Distinct().ToList();
            ViewBag.SelectedType = type;
            ViewBag.HeroImage = rooms.SelectMany(r => r.ImageUrls).Skip(1).FirstOrDefault() ?? rooms.SelectMany(r => r.ImageUrls).FirstOrDefault();
            if (!string.IsNullOrEmpty(type)) rooms = rooms.Where(r => r.RoomTypeName == type).ToList();
            return View(rooms);
        }

        // Tìm phòng trống theo khoảng ngày
        [HttpGet]
        public async Task<IActionResult> Search(DateTime checkIn, DateTime checkOut)
        {
            SetUserFlags();
            ViewBag.CheckIn = checkIn.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = checkOut.ToString("yyyy-MM-dd");
            ViewBag.IsSearchResult = true;

            var all = await LoadAllRooms();
            ViewBag.Types = all.Select(r => r.RoomTypeName).Distinct().ToList();
            ViewBag.HeroImage = all.SelectMany(r => r.ImageUrls).Skip(1).FirstOrDefault() ?? all.SelectMany(r => r.ImageUrls).FirstOrDefault();

            if (checkOut <= checkIn)
            {
                ViewBag.Error = "Ngày trả phòng phải sau ngày nhận phòng";
                return View("List", new List<RoomAvailableDTO>());
            }
            if (checkIn.Date < DateTime.Today)
            {
                ViewBag.Error = "Ngày nhận phòng không được ở quá khứ";
                return View("List", new List<RoomAvailableDTO>());
            }

            // API /available yêu cầu đăng nhập -> khách chưa đăng nhập: lọc phía web từ danh sách phòng
            List<RoomAvailableDTO> rooms;
            if (IsLoggedIn())
            {
                rooms = await GetListAsync<RoomAvailableDTO>($"/api/Rooms/available?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}");
                foreach (var r in rooms)
                {
                    var info = all.FirstOrDefault(a => a.Id == r.Id);
                    r.Description = info?.Description;
                    r.Floor = info?.Floor ?? 0;
                }
            }
            else
            {
                var nights = (checkOut.Date - checkIn.Date).Days;
                rooms = all.Where(r => r.Status != "Maintenance").ToList();
                foreach (var r in rooms) { r.Nights = nights; r.TotalPriceForStay = r.BasePrice * nights; }
                ViewBag.GuestNotice = "Đăng nhập để xem chính xác phòng còn trống và đặt phòng ngay.";
            }
            return View("List", rooms);
        }

        // Chi tiết 1 phòng + form đặt nhanh
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            SetUserFlags();
            var room = (await LoadAllRooms()).FirstOrDefault(r => r.Id == id);
            if (room == null) return RedirectToAction("Index");
            ViewBag.CheckIn = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            ViewBag.CheckOut = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");
            ViewBag.Similar = (await LoadAllRooms()).Where(r => r.RoomTypeName == room.RoomTypeName && r.Id != id).Take(3).ToList();
            return View(room);
        }
    }
}
