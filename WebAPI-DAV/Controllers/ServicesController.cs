using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(IServiceRepository serviceRepository, ILogger<ServicesController> logger)
        {
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        // GET /api/Services/get-all-services?includeInactive=false&category=Spa
        // Công khai: khách xem được danh sách dịch vụ ở trang chủ
        [HttpGet("get-all-services")]
        public IActionResult GetAll([FromQuery] bool includeInactive = false, [FromQuery] string? category = null)
        {
            return Ok(_serviceRepository.GetAll(includeInactive, category));
        }

        [HttpGet("get-service-by-id/{id}")]
        public IActionResult GetById(int id)
        {
            var result = _serviceRepository.GetById(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy dịch vụ" });
            return Ok(result);
        }

        [HttpPost("add-service")]
        [Authorize(Roles = "Manager")]
        public IActionResult Add([FromBody] AddServiceRequestDTO dto)
        {
            _logger.LogInformation($"AddService invoked with data {JsonSerializer.Serialize(dto)}");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return Ok(_serviceRepository.Add(dto));
        }

        [HttpPut("update-service-by-id/{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult Update(int id, [FromBody] AddServiceRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = _serviceRepository.Update(id, dto);
            if (result == null) return NotFound(new { message = "Không tìm thấy dịch vụ" });
            return Ok(result);
        }

        [HttpDelete("delete-service-by-id/{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult Delete(int id)
        {
            var (success, message) = _serviceRepository.Delete(id);
            if (!success) return NotFound(new { message });
            return Ok(new { message });
        }

        // ================= DỊCH VỤ KHÁCH SỬ DỤNG =================

        // GET /api/Services/usages?bookingId=5&from=2026-10-01&to=2026-10-31
        [HttpGet("usages")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult GetUsages([FromQuery] int? bookingId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(_serviceRepository.GetUsages(bookingId, from, to));
        }

        // GET /api/Services/my-usages — khách xem dịch vụ mình đã dùng
        [HttpGet("my-usages")]
        [Authorize(Roles = "Customer")]
        public IActionResult GetMyUsages()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(_serviceRepository.GetUsagesForUser(userId));
        }

        // POST /api/Services/add-usage — lễ tân ghi nhận khách dùng dịch vụ
        [HttpPost("add-usage")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult AddUsage([FromBody] AddBookingServiceRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (success, message, result) = _serviceRepository.AddUsage(dto);
            if (!success)
            {
                _logger.LogWarning($"AddUsage failed: {message}");
                return BadRequest(new { message });
            }
            return Ok(new { message, usage = result });
        }

        [HttpDelete("delete-usage/{id}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult DeleteUsage(int id)
        {
            var (success, message) = _serviceRepository.DeleteUsage(id);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
    }
}
