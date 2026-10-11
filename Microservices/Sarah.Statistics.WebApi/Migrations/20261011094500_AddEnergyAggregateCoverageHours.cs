using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sarah.Statistics.WebApi.Data;

#nullable disable

namespace Sarah.Statistics.WebApi.Migrations;

[DbContext(typeof(StatisticsDbContext))]
[Migration("20261011094500_AddEnergyAggregateCoverageHours")]
public partial class AddEnergyAggregateCoverageHours : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "CoverageHours",
            table: "EnergyAggregates",
            type: "double precision",
            nullable: false,
            defaultValue: -1.0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CoverageHours",
            table: "EnergyAggregates");
    }
}
