using Hotel_Web.Helpers;
using Hotel_Web.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Web.Controllers
{
    public class FinanceManageController : BaseApiController
    {
        public FinanceManageController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        // ================= THANH TOÁN (lịch sử giao dịch) — Manager =================
        [HttpGet]
        public async Task<IActionResult> Payments(string? status)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            ViewBag.PageTitle = "Thanh toán";
            ViewBag.FilterStatus = status;
            var payments = await GetListAsync<PaymentListDTO>("/api/Payments/get-all-payments");
            ViewBag.TotalSuccess = payments.Where(p => p.Status == "Success").Sum(p => p.Amount);
            ViewBag.CountSuccess = payments.Count(p => p.Status == "Success");
            ViewBag.CountPending = payments.Count(p => p.Status == "Pending");
            if (!string.IsNullOrEmpty(status)) payments = payments.Where(p => p.Status == status).ToList();
            return View(payments);
        }

        // ================= HÓA ĐƠN — Manager + Lễ tân (in cho khách khi trả phòng) =================
        [HttpGet]
        public async Task<IActionResult> Invoices(string? q)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Hóa đơn";
            ViewBag.Query = q;
            var bookings = (await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000"))
                .Where(b => b.Status == "CheckedOut" || b.Status == "CheckedIn").ToList();
            if (!string.IsNullOrWhiteSpace(q))
                bookings = bookings.Where(b => (b.BookingCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (b.CustomerName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            var usages = await GetListAsync<BookingServiceDTO>("/api/Services/usages");
            ViewBag.ServiceTotals = usages.GroupBy(u => u.BookingId).ToDictionary(g => g.Key, g => g.Sum(u => u.Total));
            return View(bookings.OrderByDescending(b => b.CheckOutDate).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> InvoiceDetail(int id)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var resp = await GetClient().GetAsync($"{ApiBaseUrl}/api/Bookings/get-booking-by-id/{id}");
            if (!resp.IsSuccessStatusCode) { TempData["Error"] = "Không tìm thấy booking"; return RedirectToAction("Invoices"); }
            ViewBag.PageTitle = "Hóa đơn";
            ViewBag.Services = await GetListAsync<BookingServiceDTO>($"/api/Services/usages?bookingId={id}");
            ViewBag.Transfers = await GetListAsync<RoomTransferDTO>($"/api/Bookings/transfers/{id}");
            return View(await resp.Content.ReadFromJsonAsync<BookingWithDetailDTO>());
        }

        // ================= HOÀN CỌC — Manager + Lễ tân =================
        // Danh sách booking bị hủy cần hoàn tiền cọc cho khách (kèm tài khoản khách + QR chuyển khoản nhanh)
        [HttpGet]
        public async Task<IActionResult> Refunds(string? tab)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            ViewBag.PageTitle = "Hoàn cọc";
            ViewBag.Tab = tab ?? "pending";
            var all = (await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000"))
                .Where(b => b.RefundStatus != null).ToList();
            ViewBag.PendingCount = all.Count(b => b.RefundStatus == "Pending");
            ViewBag.PendingTotal = all.Where(b => b.RefundStatus == "Pending").Sum(b => b.RefundAmount);
            ViewBag.DoneTotal = all.Where(b => b.RefundStatus == "Done").Sum(b => b.RefundAmount);
            var list = tab == "done"
                ? all.Where(b => b.RefundStatus == "Done").OrderByDescending(b => b.RefundedDate).ToList()
                : all.Where(b => b.RefundStatus == "Pending").OrderBy(b => b.CancelledDate).ToList();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> MarkRefunded(int id, string method, string? note)
        {
            var denied = CheckStaff(); if (denied != null) return denied;
            var response = await GetClient().PutAsJsonAsync($"{ApiBaseUrl}/api/Bookings/mark-refunded/{id}",
                new MarkRefundedDTO { Method = method, Note = note });
            TempData[response.IsSuccessStatusCode ? "Message" : "Error"] = await ReadError(response);
            return RedirectToAction("Refunds");
        }

        // ================= BÁO CÁO DOANH THU — Manager =================
        [HttpGet]
        public async Task<IActionResult> Revenue(int? year)
        {
            var denied = CheckManager(); if (denied != null) return denied;
            ViewBag.PageTitle = "Báo cáo doanh thu";
            var y = year ?? DateTime.Today.Year;
            ViewBag.Year = y;

            var bookings = await GetListAsync<BookingWithDetailDTO>("/api/Bookings/get-all-bookings?pageSize=5000");
            decimal Earned(BookingWithDetailDTO b) => MoneyHelper.Earned(b);

            var ofYear = bookings.Where(b => b.CreatedDate.Year == y).ToList();
            var usages = (await GetListAsync<BookingServiceDTO>($"/api/Services/usages?from={y}-01-01&to={y}-12-31"));
            var monthly = Enumerable.Range(1, 12).Select(m =>
            {
                var list = ofYear.Where(b => b.CreatedDate.Month == m).ToList();
                return new RevenueRowDTO
                {
                    Month = m,
                    BookingCount = list.Count(b => b.Status != "Cancelled" && b.Status != "NoShow"),
                    CancelledCount = list.Count(b => b.Status == "Cancelled" || b.Status == "NoShow"),
                    Deposit = list.Where(b => b.IsDepositPaid && b.Status != "Cancelled" && b.Status != "NoShow").Sum(b => b.DepositAmount),
                    ServiceRevenue = usages.Where(u => u.UsedDate.Month == m).Sum(u => u.Total),
                    CancelFee = list.Where(b => b.Status == "Cancelled" || b.Status == "NoShow").Sum(b => b.CancellationFee),
                    Revenue = list.Sum(Earned) + usages.Where(u => u.UsedDate.Month == m).Sum(u => u.Total)
                };
            }).ToList();

            ViewBag.TotalRevenue = monthly.Sum(m => m.Revenue);
            ViewBag.TotalDeposit = monthly.Sum(m => m.Deposit);
            ViewBag.TotalService = monthly.Sum(m => m.ServiceRevenue);
            ViewBag.TopServices = usages.GroupBy(u => u.ServiceName)
                .Select(g => new KeyValuePair<string, decimal>(g.Key, g.Sum(u => u.Total)))
                .OrderByDescending(x => x.Value).Take(5).ToList();
            ViewBag.TotalBookings = monthly.Sum(m => m.BookingCount);
            ViewBag.TotalCancelled = monthly.Sum(m => m.CancelledCount);
            ViewBag.Years = bookings.Select(b => b.CreatedDate.Year).Append(DateTime.Today.Year).Distinct().OrderByDescending(x => x).ToList();
            return View(monthly);
        }
    }

    public class RevenueRowDTO
    {
        public int Month { get; set; }
        public int BookingCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal Deposit { get; set; }
        public decimal ServiceRevenue { get; set; }
        public decimal CancelFee { get; set; }      // tiền cọc giữ lại khi khách hủy / không đến (có tính vào doanh thu)
        public decimal Revenue { get; set; }
    }
}
