using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI_DAV.Migrations.HotelAuthDb
{
    /// <inheritdoc />
    public partial class AddCustomerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "9a1e0e23-5b8a-4f3e-9e6a-1a2b3c4d5e6f", "9a1e0e23-5b8a-4f3e-9e6a-1a2b3c4d5e6f", "Customer", "CUSTOMER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9a1e0e23-5b8a-4f3e-9e6a-1a2b3c4d5e6f");
        }
    }
}
