using Hotel_Web.Helpers;
using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Hotel_Web.Controllers
{
    // Khu vực quản trị: Tổng quan + Quản lý phòng
    // Manager: toàn quyền | Receptionist: Dashboard, Sơ đồ phòng, Dọn dẹp & Bảo trì
    public class ManageController : BaseApiController
    {
        public ManageController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        // /Manage hoặc /Manage/Index -> chuyển về Dashboard
        [HttpGet]
        public IActionResult Index() => RedirectToAction("Dashboard");

        // ================= DASHBOARD =================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Tổng quan";

            var rooms = await GetListAsync<ManageRoomDTO>("/api/Rooms/get-all-rooms?pageSize=1000");
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            var customers = await GetListAsync<CustomerDTO>("/api/Customers/get-all-customers");
            var today = DateTime.Today;
            var live = RoomStatusHelper.BuildLive(rooms, bookings, today);

            var vm = new DashboardVM
            {
                TotalRooms = rooms.Count,
                AvailableRooms = live.Count(r => r.LiveStatus == "Available"),
                OccupiedRooms = live.Count(r => r.LiveStatus == "Occupied"),
                ReservedRooms = live.Count(r => r.LiveStatus == "Reserved"),
                MaintenanceRooms = live.Count(r => r.LiveStatus == "Maintenance"),
                TotalCustomers = customers.Count,
                PendingBookings = bookings.Count(b => b.Status == "Pending"),
                TodayArrivals = bookings.Where(b => b.CheckInDate.Date == today && (b.Status == "Confirmed" || b.Status == "Pending")).ToList(),
                TodayDepartures = bookings.Where(b => b.CheckOutDate.Date <= today && b.Status == "CheckedIn").ToList(),
                RecentBookings = bookings.OrderByDescending(b => b.CreatedDate).Take(8).ToList()
            };
            vm.ArrivalsToday = vm.TodayArrivals.Count;
            vm.DeparturesToday = vm.TodayDepartures.Count;
            vm.OccupancyPercent = vm.TotalRooms == 0 ? 0
                : (int)Math.Round(100.0 * (vm.OccupiedRooms + vm.ReservedRooms) / vm.TotalRooms);

            // Doanh thu: booking đã trả phòng tính đủ tổng tiền, booking khác đã cọc thì tính tiền cọc
            decimal Earned(BookingWithDetailDTO b) => MoneyHelper.Earned(b);
            vm.RevenueTotal = bookings.Sum(Earned);
            vm.RevenueThisMonth = bookings.Where(b => b.CreatedDate.Year == today.Year && b.CreatedDate.Month == today.Month).Sum(Earned);
            for (int i = 5; i >= 0; i--)
            {
                var m = today.AddMonths(-i);
                vm.RevenueByMonth.Add(($"T{m.Month}/{m.Year % 100:D2}",
                    bookings.Where(b => b.CreatedDate.Year == m.Year && b.CreatedDate.Month == m.Month).Sum(Earned)));
            }

            ViewBag.IsManager = IsManager();
            ViewBag.RefundPendingCount = bookings.Count(b => b.RefundStatus == "Pending");
            ViewBag.RefundPendingTotal = bookings.Where(b => b.RefundStatus == "Pending").Sum(b => b.RefundAmount);
            return View(vm);
        }

        // ================= SƠ ĐỒ PHÒNG =================
        [HttpGet]
        public async Task<IActionResult> RoomMap(DateTime? date)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Sơ đồ phòng";
            var day = date ?? DateTime.Today;
            ViewBag.SelectedDate = day.ToString("yyyy-MM-dd");

            var rooms = await GetListAsync<ManageRoomDTO>("/api/Rooms/get-all-rooms?pageSize=1000");
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            return View(RoomStatusHelper.BuildLive(rooms, bookings, day));
        }

        // ================= DANH SÁCH PHÒNG (CRUD) =================
        [HttpGet]
        public async Task<IActionResult> RoomList(string? q)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            ViewBag.PageTitle = "Danh sách phòng";
            ViewBag.Query = q;
            ViewBag.RoomTypes = await GetListAsync<RoomTypeDTO>("/api/RoomTypes/get-all-roomtypes");
            var rooms = await GetListAsync<ManageRoomDTO>("/api/Rooms/get-all-rooms?pageSize=1000&sortBy=roomnumber");
            ViewBag.OccupiedRooms = (await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?filterOn=status&filterQuery=CheckedIn&pageSize=5000"))
                .SelectMany(b => b.RoomNumbers).Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(q))
                rooms = rooms.Where(r => r.RoomNumber.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || r.RoomTypeName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            return View(rooms);
        }

        [HttpPost]
        public async Task<IActionResult> AddRoom(AddRoomDTO dto)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/Rooms/add-room", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? $"Đã thêm phòng {dto.RoomNumber}" : await ReadError(response);
            return RedirectToAction("RoomList");
        }

        [HttpPost]
        public async Task<IActionResult> EditRoom(int id, AddRoomDTO dto)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Rooms/update-room-by-id/{id}", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? $"Đã cập nhật phòng {dto.RoomNumber}" : await ReadError(response);
            return RedirectToAction("RoomList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/Rooms/delete-room-by-id/{id}");
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Đã xóa phòng" : "Không thể xóa (phòng có thể đang có booking)";
            return RedirectToAction("RoomList");
        }

        [HttpPost]
        public async Task<IActionResult> UploadRoomImage(int roomId, IFormFile imageFile)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file ảnh";
                return RedirectToAction("RoomList");
            }

            using var content = new MultipartFormDataContent();
            using var fileStream = imageFile.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(fileContent, "File", imageFile.FileName);
            content.Add(new StringContent($"room-{roomId}-{DateTime.Now.Ticks}"), "FileName");
            content.Add(new StringContent("Ảnh phòng"), "FileDescription");
            content.Add(new StringContent(roomId.ToString()), "RoomId");

            var response = await GetClient().PostAsync($"{ApiBaseUrl}/api/Images/Upload", content);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Cập nhật ảnh phòng thành công" : "Upload ảnh thất bại: " + await ReadError(response);
            return RedirectToAction("RoomList");
        }

        // ================= LOẠI PHÒNG =================
        [HttpGet]
        public async Task<IActionResult> RoomTypes()
        {
            var denied = CheckManager(); if (denied != null) return denied;
            ViewBag.PageTitle = "Phân loại phòng";
            var types = await GetListAsync<RoomTypeDTO>("/api/RoomTypes/get-all-roomtypes");
            var rooms = await GetListAsync<ManageRoomDTO>("/api/Rooms/get-all-rooms?pageSize=1000");
            ViewBag.RoomCount = rooms.GroupBy(r => r.RoomTypeName).ToDictionary(g => g.Key, g => g.Count());
            return View(types);
        }

        [HttpPost]
        public async Task<IActionResult> AddRoomType(AddRoomTypeDTO dto)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/RoomTypes/add-roomtype", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Thêm loại phòng thành công" : await ReadError(response);
            return RedirectToAction("RoomTypes");
        }

        [HttpPost]
        public async Task<IActionResult> EditRoomType(int id, AddRoomTypeDTO dto)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/RoomTypes/update-roomtype-by-id/{id}", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Cập nhật loại phòng thành công" : await ReadError(response);
            return RedirectToAction("RoomTypes");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/RoomTypes/delete-roomtype-by-id/{id}");
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] =
                response.IsSuccessStatusCode ? "Xóa loại phòng thành công" : "Không thể xóa (đang có phòng thuộc loại này)";
            return RedirectToAction("RoomTypes");
        }

        // ================= DỌN DẸP & BẢO TRÌ =================
        [HttpGet]
        public async Task<IActionResult> Housekeeping()
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Dọn dẹp & Bảo trì";
            var rooms = await GetListAsync<ManageRoomDTO>("/api/Rooms/get-all-rooms?pageSize=1000");
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            var live = RoomStatusHelper.BuildLive(rooms, bookings, DateTime.Today);

            // Phòng cần dọn: có khách trả phòng hôm nay
            ViewBag.NeedCleaning = bookings
                .Where(b => b.Status == "CheckedOut" && b.CheckOutDate.Date == DateTime.Today)
                .SelectMany(b => b.RoomNumbers).Distinct().ToList();
            return View(live);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleMaintenance(int roomId)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var client = GetClient();
            var room = await client.GetFromJsonAsync<ManageRoomDTO>($"{ApiBaseUrl}/api/Rooms/get-room-by-id/{roomId}");
            var types = await GetListAsync<RoomTypeDTO>("/api/RoomTypes/get-all-roomtypes");
            var type = types.FirstOrDefault(t => t.Name == room?.RoomTypeName);
            if (room == null || type == null)
            {
                TempData["Error"] = "Không tìm thấy phòng";
                return RedirectToAction("Housekeeping");
            }

            // Phòng đang có khách ở thì không được đưa vào dọn dẹp / bảo trì
            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?filterOn=status&filterQuery=CheckedIn&pageSize=5000");
            if (bookings.Any(b => b.Status == "CheckedIn" && b.RoomNumbers.Contains(room.RoomNumber)))
            {
                TempData["Error"] = $"Phòng {room.RoomNumber} đang có khách lưu trú, không thể đổi trạng thái. Hãy chuyển khách sang phòng khác trước.";
                return RedirectToAction("Housekeeping");
            }

            var newStatus = room.Status == "Maintenance" ? "Available" : "Maintenance";
            var dto = new AddRoomDTO { RoomNumber = room.RoomNumber, Floor = room.Floor, Status = newStatus, RoomTypeId = type.Id };
            var response = await client.PutAsJsonAsync($"{ApiBaseUrl}/api/Rooms/update-room-by-id/{roomId}", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = response.IsSuccessStatusCode
                ? (newStatus == "Maintenance" ? $"Phòng {room.RoomNumber} đã chuyển sang bảo trì" : $"Phòng {room.RoomNumber} đã sẵn sàng đón khách")
                : await ReadError(response);
            return RedirectToAction("Housekeeping");
        }
    }
}
