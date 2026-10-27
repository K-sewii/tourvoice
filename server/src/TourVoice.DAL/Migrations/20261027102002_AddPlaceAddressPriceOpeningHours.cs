using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourVoice.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceAddressPriceOpeningHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Places",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningHours",
                table: "Places",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceRange",
                table: "Places",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "OpeningHours",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "PriceRange",
                table: "Places");
        }
    }
}
