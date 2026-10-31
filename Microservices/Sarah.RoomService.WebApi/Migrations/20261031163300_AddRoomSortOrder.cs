using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.RoomService.WebApi.Migrations
{
    public partial class AddRoomSortOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH ordered_rooms AS (
                    SELECT "Id", (ROW_NUMBER() OVER (ORDER BY "Id") - 1)::integer AS position
                    FROM "Rooms"
                )
                UPDATE "Rooms" AS rooms
                SET "SortOrder" = ordered_rooms.position
                FROM ordered_rooms
                WHERE rooms."Id" = ordered_rooms."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_SortOrder",
                table: "Rooms",
                column: "SortOrder");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_SortOrder",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Rooms");
        }
    }
}
