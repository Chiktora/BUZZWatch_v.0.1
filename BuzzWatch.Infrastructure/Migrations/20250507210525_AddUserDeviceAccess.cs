using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuzzWatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeviceAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDeviceAccess",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanManage = table.Column<bool>(type: "bit", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceAccess", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceAccess_DeviceId",
                table: "UserDeviceAccess",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceAccess_UserId",
                table: "UserDeviceAccess",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceAccess_UserId_DeviceId",
                table: "UserDeviceAccess",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDeviceAccess");
        }
    }
}
