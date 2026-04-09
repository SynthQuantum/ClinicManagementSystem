using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ElapsedMilliseconds = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    RequestTimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceSamples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSamples_RequestTimestampUtc_Path",
                table: "PerformanceSamples",
                columns: new[] { "RequestTimestampUtc", "Path" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceSamples");
        }
    }
}
