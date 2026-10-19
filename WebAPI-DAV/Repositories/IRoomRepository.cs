using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public interface IRoomRepository
    {
        List<RoomDTO> GetAll(string? filterOn = null, string? filterQuery = null,
            string? sortBy = null, bool isAscending = true,
            int pageNumber = 1, int pageSize = 1000);
        List<RoomAvailableDTO> GetAvailableRooms(DateTime checkIn, DateTime checkOut, int? roomTypeId = null);
        RoomDTO? GetById(int id);
        AddRoomRequestDTO Add(AddRoomRequestDTO dto);
        AddRoomRequestDTO? Update(int id, AddRoomRequestDTO dto);
        Room? Delete(int id);
        bool IsOccupied(int roomId);
    }
}