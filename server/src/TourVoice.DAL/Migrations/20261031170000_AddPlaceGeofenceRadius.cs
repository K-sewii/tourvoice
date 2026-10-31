using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourVoice.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceGeofenceRadius : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeofenceRadiusM",
                table: "Places",
                type: "int",
                nullable: false,
                defaultValue: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeofenceRadiusM",
                table: "Places");
        }
    }
}
