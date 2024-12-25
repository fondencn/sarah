using Microsoft.EntityFrameworkCore.Migrations;

namespace Sarah.Data.Migrations
{
    public partial class DeviceTraceEntry_Values_Properties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Property",
                table: "DeviceTraces",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "DeviceTraces",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Property",
                table: "DeviceTraces");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "DeviceTraces");
        }
    }
}
