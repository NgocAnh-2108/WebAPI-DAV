using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public interface IRoomTransferRepository
    {
        (bool Success, string Message, TransferOptionsResultDTO? Result) GetOptions(int bookingId, int? fromRoomId);
        (bool Success, string Message) Transfer(int bookingId, TransferRoomRequestDTO dto);
        List<RoomTransferDTO> GetByBooking(int bookingId);
    }
}
