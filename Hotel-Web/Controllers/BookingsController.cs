using Hotel_Web.Helpers;
using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    // Chức năng của KHÁCH HÀNG: đặt phòng, xem booking, thanh toán cọc, hủy
    public class BookingsController : BaseApiController
    {
        public BookingsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        private IActionResult? CheckCustomer()
        {
            if (!IsLoggedIn())
            {
                TempData["Error"] = "Vui lòng đăng nhập để tiếp tục đặt phòng";
                return RedirectToLogin();
            }
            if (!IsInRole("Customer"))
            {
                TempData["Error"] = "Tài khoản nhân viên đặt phòng cho khách tại mục Tạo booking tại quầy";
                return RedirectToAction("NewBooking", "BookingManage");
            }
            return null;
        }

        // Trang xác nhận đặt phòng: xem lại phòng + chọn dịch vụ đặt kèm + chính sách hủy
        [HttpGet]
        public async Task<IActionResult> Confirm(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;
            if (checkOut.Date <= checkIn.Date || checkIn.Date < DateTime.Today)
            {
                TempData["Error"] = "Ngày lưu trú không hợp lệ";
                return RedirectToAction("Detail", "Rooms", new { id = roomId });
            }

            var resp = await GetClient().GetAsync($"{ApiBaseUrl}/api/Rooms/get-room-by-id/{roomId}");
            if (!resp.IsSuccessStatusCode) return RedirectToAction("Index", "Rooms");
            var room = await resp.Content.ReadFromJsonAsync<RoomRawDTO>();
            var type = (await GetListAsync<RoomTypeDTO>("/api/RoomTypes/get-all-roomtypes")).FirstOrDefault(t => t.Name == room!.RoomTypeName);

            ViewBag.Room = room;
            ViewBag.RoomType = type;
            ViewBag.CheckIn = checkIn.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = checkOut.ToString("yyyy-MM-dd");
            ViewBag.Nights = (checkOut.Date - checkIn.Date).Days;
            return View(await GetListAsync<ServiceDTO>("/api/Services/get-all-services"));
        }

        // services[ServiceId] = số lượng (0 = không chọn)
        [HttpPost]
        public async Task<IActionResult> Book(DateTime checkIn, DateTime checkOut, int roomId, string? note, Dictionary<int, int>? services)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;

            var dto = new CustomerBookingRequestDTO
            {
                CheckInDate = checkIn.Date,      // chỉ gửi NGÀY, API tự gắn giờ nhận 14:00 / trả 12:00
                CheckOutDate = checkOut.Date,
                Note = note,
                RoomIds = new List<int> { roomId },
                Services = (services ?? new()).Where(s => s.Value > 0)
                    .Select(s => new ServiceItemDTO { ServiceId = s.Key, Quantity = s.Value }).ToList()
            };

            var response = await GetClient().PostAsJsonAsync($"{ApiBaseUrl}/api/Bookings/book-room", dto);
            if (response.IsSuccessStatusCode)
            {
                // Đặt xong chuyển thẳng sang trang thanh toán cọc
                var body = await response.Content.ReadFromJsonAsync<BookResponse>();
                TempData["Message"] = "Đặt phòng thành công! Vui lòng thanh toán tiền cọc để xác nhận đặt phòng.";
                return body?.Booking != null ? RedirectToAction("Pay", new { id = body.Booking.Id }) : RedirectToAction("MyBookings");
            }

            TempData["Error"] = "Đặt phòng thất bại: " + await ReadError(response);
            return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : "/Rooms/Index");
        }

        private class BookResponse
        {
            public string? Message { get; set; }
            public BookingWithDetailDTO? Booking { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings(string? tab)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;
            ViewBag.Tab = tab ?? "upcoming";

            var response = await GetClient().GetAsync($"{ApiBaseUrl}/api/Bookings/my-bookings");
            var bookings = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<BookingWithDetailDTO>>() ?? new()
                : new();
            if (!response.IsSuccessStatusCode) ViewBag.Error = "Không tải được danh sách đặt phòng";

            // Dịch vụ khách đã sử dụng, nhóm theo booking
            var usages = await GetListAsync<BookingServiceDTO>("/api/Services/my-usages");
            ViewBag.Usages = usages.GroupBy(u => u.BookingId).ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.UpcomingCount = bookings.Count(b => !b.IsClosed);
            ViewBag.HistoryCount = bookings.Count(b => b.IsClosed);
            bookings = (tab == "history")
                ? bookings.Where(b => b.IsClosed).OrderByDescending(b => b.CheckInDate).ToList()
                : bookings.Where(b => !b.IsClosed).OrderBy(b => b.CheckInDate).ToList();
            return View(bookings);
        }

        // ================= THANH TOÁN CỌC =================
        [HttpGet]
        public async Task<IActionResult> Pay(int id)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;
            var mine = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/my-bookings");
            var booking = mine.FirstOrDefault(b => b.Id == id);
            if (booking == null || booking.IsDepositPaid || booking.Status != "Pending")
            {
                TempData["Error"] = "Booking không hợp lệ hoặc đã thanh toán cọc";
                return RedirectToAction("MyBookings");
            }
            // QR chuyển khoản MB Bank (null nếu khách sạn chưa cấu hình -> dùng QR MoMo dự phòng)
            ViewBag.Bank = await GetOneAsync<BankTransferInfoDTO>($"/api/Payments/bank-transfer-info/{id}");
            return View(booking);
        }

        // Trang thanh toán gọi mỗi vài giây: SePay báo tiền vào -> booking thành Confirmed -> trang tự chuyển
        [HttpGet]
        public async Task<IActionResult> PaymentStatus(int id)
        {
            if (!IsLoggedIn()) return Json(new { status = "", paid = false });
            var booking = (await GetListAsync<BookingWithDetailDTO>("/api/Bookings/my-bookings")).FirstOrDefault(b => b.Id == id);
            return Json(new { status = booking?.Status ?? "", paid = booking?.IsDepositPaid ?? false });
        }

        // Thanh toán cọc qua VNPay: xin link thanh toán từ API rồi chuyển khách sang cổng VNPay
        [HttpPost]
        public async Task<IActionResult> PayDeposit(int bookingId)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;

            // VNPay sẽ đưa khách quay về địa chỉ này sau khi thanh toán
            var returnUrl = Url.Action("PaymentReturn", "Bookings", null, Request.Scheme)!;
            var createResponse = await GetClient().PostAsJsonAsync(
                $"{ApiBaseUrl}/api/Payments/create-deposit-payment/{bookingId}", new CreatePaymentRequestDTO { ReturnUrl = returnUrl });
            if (!createResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = await ReadError(createResponse);
                return RedirectToAction("Pay", new { id = bookingId });
            }

            var createResult = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var dataJson = System.Text.Json.JsonSerializer.Serialize(createResult!["data"]);
            var paymentData = System.Text.Json.JsonSerializer.Deserialize<PaymentUrlResponseDTO>(dataJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Redirect(paymentData!.PaymentUrl);
        }

        // VNPay chuyển khách về đây kèm các tham số vnp_... -> gửi sang API kiểm tra chữ ký rồi xác nhận booking
        // Không bắt đăng nhập: khách có thể hết phiên trong lúc thanh toán, tính hợp lệ do chữ ký VNPay đảm bảo
        [HttpGet]
        public async Task<IActionResult> PaymentReturn()
        {
            var vnpData = Request.Query.Where(q => q.Key.StartsWith("vnp_"))
                .ToDictionary(q => q.Key, q => q.Value.ToString());
            if (!vnpData.Any()) return RedirectToAction("MyBookings");

            var response = await _httpClientFactory.CreateClient().PostAsJsonAsync($"{ApiBaseUrl}/api/Payments/vnpay-return", vnpData);
            PaymentResultDTO? result = null;
            try { result = await response.Content.ReadFromJsonAsync<PaymentResultDTO>(); } catch { }
            result ??= new PaymentResultDTO { Success = false, Message = await ReadError(response) };
            return View("PaymentResult", result);
        }

        // Khách báo đã chuyển khoản MoMo -> chờ lễ tân/quản lý đối chiếu và bấm "Thu cọc" trong trang quản trị
        [HttpPost]
        public IActionResult ReportTransfer(int bookingId)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;
            TempData["Message"] = "Cảm ơn quý khách! Lễ tân sẽ kiểm tra giao dịch MoMo và xác nhận booking trong ít phút.";
            return RedirectToAction("MyBookings");
        }

        // ================= HỦY PHÒNG =================
        // Trang xác nhận hủy: cho khách thấy trước được hoàn bao nhiêu theo chính sách
        [HttpGet]
        public async Task<IActionResult> CancelConfirm(int id)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;
            var booking = (await GetListAsync<BookingWithDetailDTO>("/api/Bookings/my-bookings")).FirstOrDefault(b => b.Id == id);
            var quote = await GetOneAsync<CancellationQuoteDTO>($"/api/Bookings/cancel-preview/{id}");
            if (booking == null || quote == null)
            {
                TempData["Error"] = "Không tìm thấy đặt phòng";
                return RedirectToAction("MyBookings");
            }
            ViewBag.Quote = quote;
            return View(booking);
        }

        // Hủy booking; nếu được hoàn cọc thì lưu luôn tài khoản nhận tiền khách vừa nhập
        [HttpPost]
        public async Task<IActionResult> Cancel(int bookingId, string? bankCode, string? accountNumber, string? accountName)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;

            // Có nhập tài khoản nhận hoàn tiền thì kiểm tra trước, sai thì chưa hủy để khách sửa lại
            if (!string.IsNullOrWhiteSpace(accountNumber) || !string.IsNullOrWhiteSpace(bankCode))
            {
                var err = ValidateRefundAccount(bankCode, accountNumber, accountName);
                if (err != null)
                {
                    TempData["Error"] = err;
                    return RedirectToAction("CancelConfirm", new { id = bookingId });
                }
            }

            var response = await GetClient().DeleteAsync($"{ApiBaseUrl}/api/Bookings/cancel/{bookingId}");
            var message = await ReadError(response);
            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(accountNumber) && !string.IsNullOrWhiteSpace(bankCode))
            {
                await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Bookings/refund-account/{bookingId}",
                    new RefundAccountDTO { BankCode = bankCode, AccountNumber = accountNumber, AccountName = accountName ?? "" });
            }
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = message;
            return RedirectToAction("MyBookings", new { tab = response.IsSuccessStatusCode ? "history" : null });
        }

        // Kiểm tra tài khoản nhận hoàn tiền: số TK chỉ gồm 6-19 chữ số, tên chỉ gồm chữ cái
        private static string? ValidateRefundAccount(string? bankCode, string? accountNumber, string? accountName)
        {
            if (string.IsNullOrWhiteSpace(bankCode) || !BankHelper.Banks.Any(b => b.Code == bankCode))
                return "Vui lòng chọn ngân hàng nhận tiền hoàn";
            if (string.IsNullOrWhiteSpace(accountNumber) || !System.Text.RegularExpressions.Regex.IsMatch(accountNumber.Trim(), @"^\d{6,19}$"))
                return "Số tài khoản chỉ được gồm chữ số (6 đến 19 số), không có chữ hay khoảng trắng";
            if (string.IsNullOrWhiteSpace(accountName) || !System.Text.RegularExpressions.Regex.IsMatch(accountName.Trim(), @"^[\p{L} ]{2,50}$"))
                return "Tên chủ tài khoản chỉ được gồm chữ cái và khoảng trắng";
            return null;
        }

        // Khách bổ sung / sửa tài khoản nhận tiền hoàn (ở trang Kỳ nghỉ của tôi)
        [HttpPost]
        public async Task<IActionResult> SaveRefundAccount(int bookingId, RefundAccountDTO dto)
        {
            var denied = CheckCustomer(); if (denied != null) return denied;
            var err = ValidateRefundAccount(dto.BankCode, dto.AccountNumber, dto.AccountName);
            if (err != null)
            {
                TempData["Error"] = err;
                return RedirectToAction("MyBookings", new { tab = "history" });
            }
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Bookings/refund-account/{bookingId}", dto);
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return RedirectToAction("MyBookings", new { tab = "history" });
        }
    }
}
