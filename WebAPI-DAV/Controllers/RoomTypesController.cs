using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomTypesController : ControllerBase
    {
        private readonly IRoomTypeRepository _roomTypeRepository;
        public RoomTypesController(IRoomTypeRepository roomTypeRepository)
        {
            _roomTypeRepository = roomTypeRepository;
        }

        [HttpGet("get-all-roomtypes")]
        public IActionResult GetAll()
        {
            return Ok(_roomTypeRepository.GetAll());
        }

        [HttpGet("get-roomtype-by-id/{id}")]
        public IActionResult GetById(int id)
        {
            var result = _roomTypeRepository.GetById(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy loại phòng" });
            return Ok(result);
        }

        [HttpPost("add-roomtype")]
        [Authorize(Roles = "Manager")]
        public IActionResult Add([FromBody] AddRoomTypeRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return Ok(_roomTypeRepository.Add(dto));
        }

        [HttpPut("update-roomtype-by-id/{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult Update(int id, [FromBody] AddRoomTypeRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = _roomTypeRepository.Update(id, dto);
            if (result == null) return NotFound(new { message = "Không tìm thấy loại phòng" });
            return Ok(result);
        }

        [HttpDelete("delete-roomtype-by-id/{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult Delete(int id)
        {
            var result = _roomTypeRepository.Delete(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy loại phòng" });
            return Ok(result);
        }
    }
}