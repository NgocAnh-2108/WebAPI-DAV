using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly UserManager<IdentityUser> _userManager;
        public CustomersController(ICustomerRepository customerRepository, UserManager<IdentityUser> userManager)
        {
            _customerRepository = customerRepository;
            _userManager = userManager;
        }

        [HttpGet("get-all-customers")]
        public IActionResult GetAll()
        {
            return Ok(_customerRepository.GetAll());
        }

        [HttpGet("get-customer-by-id/{id}")]
        public IActionResult GetById(int id)
        {
            var result = _customerRepository.GetById(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy khách hàng" });
            return Ok(result);
        }

        [HttpPost("add-customer")]
        [Authorize(Roles = "Manager,Receptionist")]
        public IActionResult Add([FromBody] AddCustomerRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return Ok(_customerRepository.Add(dto));
        }

        [HttpPut("update-customer-by-id/{id}")]
        [Authorize(Roles = "Manager,Receptionist")]
        public IActionResult Update(int id, [FromBody] AddCustomerRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = _customerRepository.Update(id, dto);
            if (result == null) return NotFound(new { message = "Không tìm thấy khách hàng" });
            return Ok(result);
        }

        // Lễ tân và Quản lý đều xóa được khách hàng (cấp dưới của cả hai).
        // Khách đã có booking thì không cho xóa; khách có tài khoản đăng nhập thì xóa luôn tài khoản đó.
        [HttpDelete("delete-customer-by-id/{id}")]
        [Authorize(Roles = "Manager,Receptionist")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message, userId) = _customerRepository.SafeDelete(id);
            if (!success) return BadRequest(new { message });

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                    message += " và tài khoản đăng nhập của khách";
                }
            }
            return Ok(new { message });
        }
    }
}