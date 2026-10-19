using WebAPI_DAV.Data;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public class SQLCustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLCustomerRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<CustomerDTO> GetAll()
        {
            return _dbContext.Customers.Select(c => new CustomerDTO
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                IdentityCard = c.IdentityCard,
                Address = c.Address
            }).ToList();
        }

        public CustomerDTO? GetById(int id)
        {
            var c = _dbContext.Customers.FirstOrDefault(x => x.Id == id);
            if (c == null) return null;
            return new CustomerDTO
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                IdentityCard = c.IdentityCard,
                Address = c.Address
            };
        }

        public AddCustomerRequestDTO Add(AddCustomerRequestDTO dto)
        {
            var domain = new Customer
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                IdentityCard = dto.IdentityCard,
                Address = dto.Address
            };
            _dbContext.Customers.Add(domain);
            _dbContext.SaveChanges();
            return dto;
        }

        public AddCustomerRequestDTO? Update(int id, AddCustomerRequestDTO dto)
        {
            var domain = _dbContext.Customers.FirstOrDefault(x => x.Id == id);
            if (domain == null) return null;
            domain.FullName = dto.FullName;
            domain.Phone = dto.Phone;
            domain.Email = dto.Email;
            domain.IdentityCard = dto.IdentityCard;
            domain.Address = dto.Address;
            _dbContext.SaveChanges();
            return dto;
        }

        public Customer? Delete(int id)
        {
            var domain = _dbContext.Customers.FirstOrDefault(x => x.Id == id);
            if (domain != null)
            {
                _dbContext.Customers.Remove(domain);
                _dbContext.SaveChanges();
            }
            return domain;
        }

        public AddCustomerRequestDTO AddFromRegister(AddCustomerRequestDTO dto, string userId)
        {
            var domain = new Customer
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                UserId = userId
            };
            _dbContext.Customers.Add(domain);
            _dbContext.SaveChanges();
            return dto;
        }

        public Customer? GetByUserId(string userId)
        {
            return _dbContext.Customers.FirstOrDefault(c => c.UserId == userId);
        }

        public (bool Success, string Message, string? UserId) SafeDelete(int id)
        {
            var domain = _dbContext.Customers.FirstOrDefault(x => x.Id == id);
            if (domain == null) return (false, "Không tìm thấy khách hàng", null);

            var bookingCount = _dbContext.Bookings.Count(b => b.CustomerID == id);
            if (bookingCount > 0)
                return (false, $"Khách hàng đã có {bookingCount} booking, không thể xóa (giữ lịch sử và hóa đơn)", null);

            var userId = domain.UserId;
            _dbContext.Customers.Remove(domain);
            _dbContext.SaveChanges();
            return (true, $"Đã xóa khách hàng {domain.FullName}", userId);
        }

        public void UnlinkOrDeleteByUserId(string userId)
        {
            var domain = _dbContext.Customers.FirstOrDefault(c => c.UserId == userId);
            if (domain == null) return;

            if (_dbContext.Bookings.Any(b => b.CustomerID == domain.Id))
                domain.UserId = null;               // giữ hồ sơ + lịch sử booking, chỉ bỏ liên kết tài khoản
            else
                _dbContext.Customers.Remove(domain);
            _dbContext.SaveChanges();
        }
    }
}