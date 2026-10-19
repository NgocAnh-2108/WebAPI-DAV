using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public interface ICustomerRepository
    {
        List<CustomerDTO> GetAll();
        CustomerDTO? GetById(int id);
        AddCustomerRequestDTO Add(AddCustomerRequestDTO dto);
        AddCustomerRequestDTO? Update(int id, AddCustomerRequestDTO dto);
        Customer? Delete(int id);

        AddCustomerRequestDTO AddFromRegister(AddCustomerRequestDTO dto, string userId);
        Customer? GetByUserId(string userId);

        // Xóa an toàn: không cho xóa khách đã có booking (tránh mất lịch sử/hóa đơn)
        (bool Success, string Message, string? UserId) SafeDelete(int id);
        // Khi xóa tài khoản đăng nhập của khách: còn booking -> chỉ gỡ liên kết, chưa có booking -> xóa hồ sơ
        void UnlinkOrDeleteByUserId(string userId);
    }
}