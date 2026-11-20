using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConsumptionManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    MeterNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meters_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeterId = table.Column<int>(type: "int", nullable: false),
                    ReadingSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TReadingimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeterReadings_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Sites",
                columns: new[] { "Id", "Address", "Name" },
                values: new object[] { 1, "100 Industrial Way", "Main Facility" });

            migrationBuilder.InsertData(
                table: "Meters",
                columns: new[] { "Id", "MeterNumber", "SiteId" },
                values: new object[] { 1, "MTR-1001", 1 });

            migrationBuilder.InsertData(
                table: "MeterReadings",
                columns: new[] { "Id", "MeterId", "ReadingSum", "TReadingimeUtc" },
                values: new object[,]
                {
                    { 1, 1, 1000.0000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 1, 1012.5000m, new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 1, 1030.0000m, new DateTime(2026, 1, 1, 2, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId_TReadingimeUtc",
                table: "MeterReadings",
                columns: new[] { "MeterId", "TReadingimeUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meters_MeterNumber",
                table: "Meters",
                column: "MeterNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meters_SiteId",
                table: "Meters",
                column: "SiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeterReadings");

            migrationBuilder.DropTable(
                name: "Meters");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
