using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebAPI_DAV.Data
{
    public class HotelAuthDbContext : IdentityDbContext
    {
        public HotelAuthDbContext(DbContextOptions<HotelAuthDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var receptionistRoleId = "004c7e80-7dfc-44be-8952-2c7130898655";
            var managerRoleId = "71e282d3-76ca-485e-b094-eff019287fa5";
            var customerRoleId = "9a1e0e23-5b8a-4f3e-9e6a-1a2b3c4d5e6f";
            base.OnModelCreating(builder);

            var roles = new List<IdentityRole>
    {
        new IdentityRole { Id = receptionistRoleId, ConcurrencyStamp = receptionistRoleId, Name = "Receptionist", NormalizedName = "Receptionist".ToUpper() },
        new IdentityRole { Id = managerRoleId, ConcurrencyStamp = managerRoleId, Name = "Manager", NormalizedName = "Manager".ToUpper() },
        new IdentityRole { Id = customerRoleId, ConcurrencyStamp = customerRoleId, Name = "Customer", NormalizedName = "Customer".ToUpper() }
    };

            builder.Entity<IdentityRole>().HasData(roles);
        }
    }
}