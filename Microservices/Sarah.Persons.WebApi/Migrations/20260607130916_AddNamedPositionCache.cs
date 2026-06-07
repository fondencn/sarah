using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sarah.Persons.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNamedPositionCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NamedPositionCache",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Latitude = table.Column<decimal>(type: "numeric(9,5)", precision: 9, scale: 5, nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(9,5)", precision: 9, scale: 5, nullable: false),
                    NamedLocation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamedPositionCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NamedPositionCache_Latitude_Longitude",
                table: "NamedPositionCache",
                columns: new[] { "Latitude", "Longitude" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NamedPositionCache");
        }
    }
}
