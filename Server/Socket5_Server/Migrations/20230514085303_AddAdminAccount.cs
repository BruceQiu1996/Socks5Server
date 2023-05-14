using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Socks5_Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DownloadBytes", "ExpireTime", "Password", "Role", "UploadBytes", "UserName" },
                values: new object[] { "1ce5a888-d236-4e0b-9a65-5adbcfd2baf8", 0L, new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), "123456", (byte)99, 0L, "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "1ce5a888-d236-4e0b-9a65-5adbcfd2baf8");
        }
    }
}
