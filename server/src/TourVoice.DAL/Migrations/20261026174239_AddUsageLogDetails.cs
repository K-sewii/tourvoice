using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourVoice.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageLogDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Chunks",
                table: "UsageLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chunks",
                table: "UsageLogs");
        }
    }
}
