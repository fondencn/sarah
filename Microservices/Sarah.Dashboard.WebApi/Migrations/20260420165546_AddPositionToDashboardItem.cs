using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.Dashboard.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionToDashboardItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "DashboardItems");
        }
    }
}
