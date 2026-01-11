using Microsoft.EntityFrameworkCore.Migrations;

namespace Sarah.Data.Migrations
{
    public partial class TemperatureSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemperatureSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id_Room = table.Column<long>(nullable: true),
                    Weekday = table.Column<long>(type: "int", nullable: false),
                    Hour = table.Column<byte>(type: "int", nullable: false),
                    Minute = table.Column<byte>(type: "int", nullable: false),
                    TemperatureSetpoint = table.Column<byte>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureSchedules", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemperatureSchedules");
        }
    }
}
