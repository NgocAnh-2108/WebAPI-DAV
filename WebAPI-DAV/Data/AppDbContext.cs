using Microsoft.EntityFrameworkCore;
using WebAPI_DAV.Models.Domain;

namespace WebAPI_DAV.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Quan hệ n-n giữa Booking và Room thông qua BookingRoom
            modelBuilder.Entity<BookingRoom>()
                .HasOne(b => b.Booking)
                .WithMany(br => br.BookingRooms)
                .HasForeignKey(bi => bi.BookingId);

            modelBuilder.Entity<BookingRoom>()
                .HasOne(b => b.Room)
                .WithMany(br => br.BookingRooms)
                .HasForeignKey(bi => bi.RoomId);

            // Quan hệ n-n giữa Booking và Service thông qua BookingService
            modelBuilder.Entity<BookingService>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingServices)
                .HasForeignKey(bs => bs.BookingId);

            modelBuilder.Entity<BookingService>()
                .HasOne(bs => bs.Service)
                .WithMany(s => s.BookingServices)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Lịch sử chuyển phòng: 2 khóa ngoại cùng trỏ tới Room -> phải Restrict để tránh nhiều đường xóa dây chuyền
            modelBuilder.Entity<RoomTransfer>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.RoomTransfers)
                .HasForeignKey(t => t.BookingId);

            modelBuilder.Entity<RoomTransfer>()
                .HasOne(t => t.FromRoom)
                .WithMany()
                .HasForeignKey(t => t.FromRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomTransfer>()
                .HasOne(t => t.ToRoom)
                .WithMany()
                .HasForeignKey(t => t.ToRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomTransfer>().Property(t => t.OldPricePerNight).HasPrecision(18, 2);
            modelBuilder.Entity<RoomTransfer>().Property(t => t.NewPricePerNight).HasPrecision(18, 2);
            modelBuilder.Entity<RoomTransfer>().Property(t => t.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Booking>().Property(b => b.CancellationFee).HasPrecision(18, 2);
            modelBuilder.Entity<Booking>().Property(b => b.RefundAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Booking>().Property(b => b.RoomChangeDiscount).HasPrecision(18, 2);

            modelBuilder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);
            modelBuilder.Entity<BookingService>().Property(bs => bs.UnitPrice).HasPrecision(18, 2);

            // Dữ liệu mẫu cho bảng dịch vụ
            modelBuilder.Entity<Service>().HasData(
                new Service { Id = 1, Name = "Buffet sáng", Description = "Buffet Á - Âu tại nhà hàng tầng 1, 6:00 - 10:00", Category = "Ẩm thực", Price = 250000, Unit = "suất", IsActive = true },
                new Service { Id = 2, Name = "Ăn tối tại phòng", Description = "Thực đơn set menu phục vụ tận phòng", Category = "Ẩm thực", Price = 450000, Unit = "suất", IsActive = true },
                new Service { Id = 3, Name = "Massage toàn thân", Description = "Liệu trình 60 phút với tinh dầu thiên nhiên", Category = "Spa & Thư giãn", Price = 600000, Unit = "lần", IsActive = true },
                new Service { Id = 4, Name = "Giặt ủi", Description = "Giặt sấy và ủi, trả trong ngày", Category = "Giặt ủi", Price = 50000, Unit = "kg", IsActive = true },
                new Service { Id = 5, Name = "Đưa đón sân bay", Description = "Xe 4-7 chỗ đưa đón sân bay Liên Khương", Category = "Di chuyển", Price = 300000, Unit = "chuyến", IsActive = true },
                new Service { Id = 6, Name = "Thuê xe máy", Description = "Xe tay ga, kèm 2 mũ bảo hiểm", Category = "Di chuyển", Price = 150000, Unit = "ngày", IsActive = true },
                new Service { Id = 7, Name = "Minibar", Description = "Nước uống và đồ ăn nhẹ trong phòng", Category = "Ẩm thực", Price = 30000, Unit = "món", IsActive = true }
            );
        }

        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingRoom> BookingRooms { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<BookingService> BookingServices { get; set; }
        public DbSet<RoomTransfer> RoomTransfers { get; set; }
    }
}