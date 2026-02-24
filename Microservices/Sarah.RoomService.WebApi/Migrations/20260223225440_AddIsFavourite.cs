using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.RoomService.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFavourite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavourite",
                table: "Rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavourite",
                table: "Rooms");
        }
    }
}
