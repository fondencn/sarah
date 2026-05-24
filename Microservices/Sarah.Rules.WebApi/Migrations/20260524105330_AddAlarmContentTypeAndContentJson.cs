using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.Rules.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmContentTypeAndContentJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentJson",
                table: "AlarmSchedules",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentType",
                table: "AlarmSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentJson",
                table: "AlarmSchedules");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "AlarmSchedules");
        }
    }
}
