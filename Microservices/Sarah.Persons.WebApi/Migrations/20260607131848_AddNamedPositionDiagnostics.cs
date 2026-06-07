using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.Persons.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNamedPositionDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "NamedPositionCache",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NominatimJson",
                table: "NamedPositionCache",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "NamedPositionCache");

            migrationBuilder.DropColumn(
                name: "NominatimJson",
                table: "NamedPositionCache");
        }
    }
}
