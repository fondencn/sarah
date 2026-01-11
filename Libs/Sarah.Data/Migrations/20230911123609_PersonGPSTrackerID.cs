using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonGPSTrackerID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "GPSTrackerID",
                table: "Persons",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GPSTrackerID",
                table: "Persons");
        }
    }
}
