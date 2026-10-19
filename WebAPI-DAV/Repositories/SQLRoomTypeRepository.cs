using WebAPI_DAV.Data;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public class SQLRoomTypeRepository : IRoomTypeRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLRoomTypeRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<RoomTypeDTO> GetAll()
        {
            return _dbContext.RoomTypes.Select(rt => new RoomTypeDTO
            {
                Id = rt.Id,
                Name = rt.Name,
                Description = rt.Description,
                BasePrice = rt.BasePrice,
                MaxOccupancy = rt.MaxOccupancy
            }).ToList();
        }

        public RoomTypeDTO? GetById(int id)
        {
            var rt = _dbContext.RoomTypes.FirstOrDefault(x => x.Id == id);
            if (rt == null) return null;
            return new RoomTypeDTO
            {
                Id = rt.Id,
                Name = rt.Name,
                Description = rt.Description,
                BasePrice = rt.BasePrice,
                MaxOccupancy = rt.MaxOccupancy
            };
        }

        public AddRoomTypeRequestDTO Add(AddRoomTypeRequestDTO dto)
        {
            var domain = new RoomType
            {
                Name = dto.Name,
                Description = dto.Description,
                BasePrice = dto.BasePrice,
                MaxOccupancy = dto.MaxOccupancy
            };
            _dbContext.RoomTypes.Add(domain);
            _dbContext.SaveChanges();
            return dto;
        }

        public AddRoomTypeRequestDTO? Update(int id, AddRoomTypeRequestDTO dto)
        {
            var domain = _dbContext.RoomTypes.FirstOrDefault(x => x.Id == id);
            if (domain == null) return null;
            domain.Name = dto.Name;
            domain.Description = dto.Description;
            domain.BasePrice = dto.BasePrice;
            domain.MaxOccupancy = dto.MaxOccupancy;
            _dbContext.SaveChanges();
            return dto;
        }

        public RoomType? Delete(int id)
        {
            var domain = _dbContext.RoomTypes.FirstOrDefault(x => x.Id == id);
            if (domain != null)
            {
                _dbContext.RoomTypes.Remove(domain);
                _dbContext.SaveChanges();
            }
            return domain;
        }
    }
}