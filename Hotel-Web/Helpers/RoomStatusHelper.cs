using Hotel_Web.Models.DTO;

namespace Hotel_Web.Helpers
{
    public static class RoomStatusHelper
    {
        // Booking "đang chiếm" phòng vào ngày `day`: CheckIn <= day < CheckOut và chưa hủy/trả
        // Khách đã check-in nhưng chưa làm thủ tục trả phòng vẫn tính là đang chiếm phòng hôm nay
        public static bool Covers(BookingWithDetailDTO b, DateTime day)
        {
            if (b.Status == "Cancelled" || b.Status == "CheckedOut" || b.Status == "NoShow") return false;
            if (b.CheckInDate.Date > day.Date) return false;
            if (b.CheckOutDate.Date > day.Date) return true;
            return b.Status == "CheckedIn" && day.Date == DateTime.Today;
        }

        public static List<RoomLiveDTO> BuildLive(List<ManageRoomDTO> rooms, List<BookingWithDetailDTO> bookings, DateTime day)
        {
            var result = new List<RoomLiveDTO>();
            foreach (var r in rooms)
            {
                var live = new RoomLiveDTO
                {
                    Id = r.Id, RoomNumber = r.RoomNumber, Floor = r.Floor, RoomTypeName = r.RoomTypeName,
                    BasePrice = r.BasePrice, ImageUrl = r.ImageUrl, RoomStatus = r.Status
                };

                // Đang có khách (CheckedIn) được ưu tiên, sau đó tới đặt trước
                var bk = bookings
                    .Where(b => b.RoomNumbers.Contains(r.RoomNumber) && Covers(b, day))
                    .OrderByDescending(b => b.Status == "CheckedIn")
                    .FirstOrDefault();

                if (r.Status == "Maintenance") live.LiveStatus = "Maintenance";
                else if (bk != null) live.LiveStatus = bk.Status == "CheckedIn" ? "Occupied" : "Reserved";
                else live.LiveStatus = "Available";

                // Phòng đang có khách lưu trú -> không được đổi trạng thái dọn dẹp / bảo trì
                live.HasGuest = bookings.Any(b => b.Status == "CheckedIn" && b.RoomNumbers.Contains(r.RoomNumber));
                live.GuestName = bk?.CustomerName;
                live.BookingCode = bk?.BookingCode;
                result.Add(live);
            }
            return result.OrderBy(x => x.Floor).ThenBy(x => x.RoomNumber).ToList();
        }
    }
}
