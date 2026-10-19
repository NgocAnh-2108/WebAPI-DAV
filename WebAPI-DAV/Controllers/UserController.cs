using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITokenRepository _tokenRepository;
        private readonly ICustomerRepository _customerRepository;
        public UserController(UserManager<IdentityUser> userManager, ITokenRepository tokenRepository,
            ICustomerRepository customerRepository)
        {
            _userManager = userManager;
            _tokenRepository = tokenRepository;
            _customerRepository = customerRepository; // FIX: trước đây quên gán -> lỗi null khi đăng ký Customer
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequestDTO)
        {
            var identityUser = new IdentityUser
            {
                UserName = registerRequestDTO.Username,
                Email = registerRequestDTO.Username
            };
            var identityResult = await _userManager.CreateAsync(identityUser, registerRequestDTO.Password);

            if (identityResult.Succeeded)
            {
                if (registerRequestDTO.Roles != null && registerRequestDTO.Roles.Any())
                {
                    identityResult = await _userManager.AddToRolesAsync(identityUser, registerRequestDTO.Roles);
                }
                if (identityResult.Succeeded)
                {
                    // Nếu đăng ký với role Customer, tự tạo hồ sơ Customer liên kết với UserId
                    if (registerRequestDTO.Roles != null && registerRequestDTO.Roles.Contains("Customer"))
                    {
                        _customerRepository.AddFromRegister(new Models.DTO.AddCustomerRequestDTO
                        {
                            FullName = registerRequestDTO.FullName ?? registerRequestDTO.Username,
                            Phone = registerRequestDTO.Phone ?? "",
                            Email = registerRequestDTO.Username
                        }, identityUser.Id);
                    }
                    return Ok("Register Successful! Let login!");
                }
            }
            return BadRequest("Something wrong!");
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequestDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginRequestDTO.Username);
            if (user != null)
            {
                var checkPasswordResult = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);
                if (checkPasswordResult)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles != null)
                    {
                        var jwtToken = _tokenRepository.CreateJWTToken(user, roles.ToList());
                        var response = new LoginResponseDTO
                        {
                            JwtToken = jwtToken
                        };
                        return Ok(response);
                    }
                }
            }
            return BadRequest("Username or password incorrect");
        }
        // POST /api/User/change-password — mọi tài khoản đã đăng nhập tự đổi mật khẩu của mình
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản" });

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng hoặc mật khẩu mới chưa hợp lệ (tối thiểu 6 ký tự)" });
            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        // ================= PHÂN CẤP QUYỀN =================
        // Manager (3) > Receptionist (2) > Customer (1)
        // Mỗi cấp chỉ được xem/xóa tài khoản THẤP HƠN mình.
        private static int RoleRank(IEnumerable<string> roles)
        {
            if (roles.Contains("Manager")) return 3;
            if (roles.Contains("Receptionist")) return 2;
            if (roles.Contains("Customer")) return 1;
            return 0;
        }

        private int CurrentUserRank()
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            return RoleRank(roles);
        }

        [HttpGet("get-all-accounts")]
        [Authorize(Roles = "Manager,Receptionist")]
        public async Task<IActionResult> GetAllAccounts()
        {
            var myRank = CurrentUserRank();
            var myId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var users = _userManager.Users.ToList();
            var result = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var rank = RoleRank(roles);
                // Manager thấy tất cả; Lễ tân chỉ thấy tài khoản khách hàng
                if (myRank < 3 && rank >= myRank) continue;
                foreach (var role in roles)
                {
                    result.Add(new
                    {
                        Id = user.Id,
                        Email = user.Email,
                        RoleName = role,
                        CanDelete = rank < myRank && user.Id != myId
                    });
                }
            }
            return Ok(result);
        }

        // DELETE /api/User/delete-account/{id}
        // Manager xóa được Lễ tân + Khách hàng; Lễ tân xóa được Khách hàng; không ai xóa được cấp bằng/cao hơn mình
        [HttpDelete("delete-account/{id}")]
        [Authorize(Roles = "Manager,Receptionist")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            var myId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == myId) return BadRequest(new { message = "Không thể tự xóa tài khoản của chính mình" });

            var target = await _userManager.FindByIdAsync(id);
            if (target == null) return NotFound(new { message = "Không tìm thấy tài khoản" });

            var targetRoles = await _userManager.GetRolesAsync(target);
            if (RoleRank(targetRoles) >= CurrentUserRank())
                return StatusCode(403, new { message = "Bạn chỉ được xóa tài khoản có quyền thấp hơn mình" });

            // Tài khoản khách: xử lý hồ sơ Customer liên kết trước (giữ lịch sử booking nếu có)
            if (targetRoles.Contains("Customer"))
                _customerRepository.UnlinkOrDeleteByUserId(target.Id);

            var result = await _userManager.DeleteAsync(target);
            if (!result.Succeeded) return BadRequest(new { message = "Xóa tài khoản thất bại" });
            return Ok(new { message = $"Đã xóa tài khoản {target.Email}" });
        }
    }
}