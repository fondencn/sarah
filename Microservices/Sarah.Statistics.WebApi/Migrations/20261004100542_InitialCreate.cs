using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sarah.Statistics.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnergyAggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Granularity = table.Column<int>(type: "integer", nullable: false),
                    BucketStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvgPowerW = table.Column<double>(type: "double precision", nullable: false),
                    MaxPowerW = table.Column<double>(type: "double precision", nullable: false),
                    EnergyKwh = table.Column<double>(type: "double precision", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnergySamples",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PowerW = table.Column<double>(type: "double precision", nullable: false),
                    EnergyKwhTotal = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergySamples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnergyAggregates_DeviceId_Granularity_BucketStart",
                table: "EnergyAggregates",
                columns: new[] { "DeviceId", "Granularity", "BucketStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnergyAggregates_Granularity_BucketStart",
                table: "EnergyAggregates",
                columns: new[] { "Granularity", "BucketStart" });

            migrationBuilder.CreateIndex(
                name: "IX_EnergySamples_DeviceId_Timestamp",
                table: "EnergySamples",
                columns: new[] { "DeviceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_EnergySamples_Timestamp",
                table: "EnergySamples",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnergyAggregates");

            migrationBuilder.DropTable(
                name: "EnergySamples");
        }
    }
}
