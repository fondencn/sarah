using Microsoft.EntityFrameworkCore.Migrations;

namespace Sarah.Data.Migrations
{
    public partial class AlarmScheduleRecurrence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerializedRecurrence",
                table: "AlarmSchedules",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerializedRecurrence",
                table: "AlarmSchedules");
        }
    }
}
