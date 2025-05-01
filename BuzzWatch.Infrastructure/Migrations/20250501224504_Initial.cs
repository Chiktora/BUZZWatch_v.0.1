using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuzzWatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location_Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location_Latitude = table.Column<double>(type: "float", nullable: true),
                    Location_Longitude = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Headers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Headers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Headers_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementHumIn",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ValuePct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementHumIn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementHumIn_Headers_Id",
                        column: x => x.Id,
                        principalTable: "Headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementHumOut",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ValuePct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementHumOut", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementHumOut_Headers_Id",
                        column: x => x.Id,
                        principalTable: "Headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementTempIn",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ValueC = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementTempIn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementTempIn_Headers_Id",
                        column: x => x.Id,
                        principalTable: "Headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementTempOut",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ValueC = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementTempOut", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementTempOut_Headers_Id",
                        column: x => x.Id,
                        principalTable: "Headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementWeight",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ValueKg = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementWeight", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementWeight_Headers_Id",
                        column: x => x.Id,
                        principalTable: "Headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CreatedAt",
                table: "Devices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Headers_DeviceId",
                table: "Headers",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Headers_RecordedAt",
                table: "Headers",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasurementHumIn");

            migrationBuilder.DropTable(
                name: "MeasurementHumOut");

            migrationBuilder.DropTable(
                name: "MeasurementTempIn");

            migrationBuilder.DropTable(
                name: "MeasurementTempOut");

            migrationBuilder.DropTable(
                name: "MeasurementWeight");

            migrationBuilder.DropTable(
                name: "Headers");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
