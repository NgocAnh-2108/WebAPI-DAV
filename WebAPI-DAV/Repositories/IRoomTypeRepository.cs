using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public interface IRoomTypeRepository
    {
        List<RoomTypeDTO> GetAll();
        RoomTypeDTO? GetById(int id);
        AddRoomTypeRequestDTO Add(AddRoomTypeRequestDTO dto);
        AddRoomTypeRequestDTO? Update(int id, AddRoomTypeRequestDTO dto);
        RoomType? Delete(int id);
    }
}