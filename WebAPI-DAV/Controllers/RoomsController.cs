using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomRepository _roomRepository;
        public RoomsController(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        // GET: /api/Rooms/get-all-rooms?filterOn=status&filterQuery=Available&sortBy=price&isAscending=true&pageNumber=1&pageSize=100
        [HttpGet("get-all-rooms")]
        public IActionResult GetAll([FromQuery] string? filterOn, [FromQuery] string? filterQuery,
            [FromQuery] string? sortBy, [FromQuery] bool isAscending = true,
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            var allRooms = _roomRepository.GetAll(filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);
            return Ok(allRooms);
        }

        [HttpGet("get-room-by-id/{id}")]
        public IActionResult GetById(int id)
        {
            var result = _roomRepository.GetById(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy phòng" });
            return Ok(result);
        }

        [HttpPost("add-room")]
        [Authorize(Roles = "Manager")]
        public IActionResult Add([FromBody] AddRoomRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return Ok(_roomRepository.Add(dto));
        }

        [HttpPut("update-room-by-id/{id}")]
        [Authorize(Roles = "Manager,Receptionist")]
        public IActionResult Update(int id, [FromBody] AddRoomRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Phòng đang có khách ở: không cho chuyển sang dọn dẹp / bảo trì (phải chuyển khách sang phòng khác trước)
            var current = _roomRepository.GetById(id);
            if (current != null && current.Status != dto.Status && _roomRepository.IsOccupied(id))
                return BadRequest(new { message = $"Phòng {current.RoomNumber} đang có khách lưu trú, không thể đổi trạng thái dọn dẹp/bảo trì. Hãy chuyển khách sang phòng khác trước." });

            var result = _roomRepository.Update(id, dto);
            if (result == null) return NotFound(new { message = "Không tìm thấy phòng" });
            return Ok(result);
        }

        [HttpDelete("delete-room-by-id/{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult Delete(int id)
        {
            var result = _roomRepository.Delete(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy phòng" });
            return Ok(result);
        }

        [HttpGet("available")]
        [Authorize(Roles = "Customer,Receptionist,Manager")]
        public IActionResult GetAvailableRooms([FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut, [FromQuery] int? roomTypeId)
        {
            var result = _roomRepository.GetAvailableRooms(checkIn, checkOut, roomTypeId);
            return Ok(result);
        }
    }
}