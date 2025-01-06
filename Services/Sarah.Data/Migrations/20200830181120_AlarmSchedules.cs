using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sarah.Data.Migrations
{
    public partial class AlarmSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlarmTime = table.Column<DateTime>(nullable: false),
                    Text = table.Column<string>(nullable: false),
                    TargetSpeaker = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmSchedules", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmSchedules");
        }
    }
}
