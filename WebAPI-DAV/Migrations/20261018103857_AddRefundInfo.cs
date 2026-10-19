using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI_DAV.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundAccountName",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountNumber",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankCode",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundNote",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundAccountName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundAccountNumber",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundBankCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundNote",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundedDate",
                table: "Bookings");
        }
    }
}
