using WebAPI_DAV.Helpers;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;

namespace WebAPI_DAV.Repositories
{
    public interface IBookingRepository
    {
        List<BookingWithDetailDTO> GetAll(string? filterOn = null, string? filterQuery = null,
            string? sortBy = null, bool isAscending = true,
            int pageNumber = 1, int pageSize = 1000);
        BookingWithDetailDTO? GetById(int id);
        (bool Success, string Message, BookingWithDetailDTO? Result) Add(AddBookingRequestDTO dto);
        Booking? Delete(int id);
        (bool Success, string Message) UpdateStatus(int id, string status);

        (bool Success, string Message, BookingWithDetailDTO? Result) AddForCustomer(CustomerBookingRequestDTO dto, string userId, ICustomerRepository customerRepository);
        List<BookingWithDetailDTO> GetMyBookings(string userId);

        BookingWithDetailDTO? GetByBookingCode(string bookingCode);
        (bool Success, string Message) ConfirmDepositAtDesk(int bookingId);

        // Chính sách hủy phòng
        (bool Success, string Message, CancellationQuote? Quote) GetCancelQuote(int bookingId, string? userId, bool byHotel = false);
        (bool Success, string Message) CancelMyBooking(int bookingId, string userId);
        (bool Success, string Message) StaffCancel(int bookingId, StaffCancelRequestDTO dto);
        (bool Success, string Message) MarkNoShow(int bookingId);
        (bool Success, string Message) MarkRefunded(int bookingId, MarkRefundedDTO dto);
        (bool Success, string Message) SetRefundAccount(int bookingId, string userId, RefundAccountDTO dto);

        // Đổi giờ nhận / trả phòng
        (bool Success, string Message) ChangeTime(int bookingId, ChangeBookingTimeDTO dto);
    }
}
