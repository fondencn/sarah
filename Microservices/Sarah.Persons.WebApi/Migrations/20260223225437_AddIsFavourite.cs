using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sarah.Persons.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFavourite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavourite",
                table: "Persons",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavourite",
                table: "Persons");
        }
    }
}
