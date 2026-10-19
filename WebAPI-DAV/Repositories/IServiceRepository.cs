using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public interface IServiceRepository
    {
        List<ServiceDTO> GetAll(bool includeInactive = false, string? category = null);
        ServiceDTO? GetById(int id);
        AddServiceRequestDTO Add(AddServiceRequestDTO dto);
        AddServiceRequestDTO? Update(int id, AddServiceRequestDTO dto);
        (bool Success, string Message) Delete(int id);

        // Dịch vụ khách sử dụng
        List<BookingServiceDTO> GetUsages(int? bookingId = null, DateTime? from = null, DateTime? to = null);
        List<BookingServiceDTO> GetUsagesForUser(string userId);
        (bool Success, string Message, BookingServiceDTO? Result) AddUsage(AddBookingServiceRequestDTO dto);
        (bool Success, string Message) DeleteUsage(int id);
    }
}
