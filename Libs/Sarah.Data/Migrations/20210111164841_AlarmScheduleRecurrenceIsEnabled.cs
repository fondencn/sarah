using Microsoft.EntityFrameworkCore.Migrations;

namespace Sarah.Data.Migrations
{
    public partial class AlarmScheduleRecurrenceIsEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasRecurrence",
                table: "AlarmSchedules",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasRecurrence",
                table: "AlarmSchedules");
        }
    }
}
