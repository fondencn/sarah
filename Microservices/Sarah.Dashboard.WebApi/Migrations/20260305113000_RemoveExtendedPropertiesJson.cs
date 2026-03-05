using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.Dashboard.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExtendedPropertiesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtendedPropertiesJson",
                table: "DashboardItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtendedPropertiesJson",
                table: "DashboardItems",
                type: "jsonb",
                nullable: true);
        }
    }
}
