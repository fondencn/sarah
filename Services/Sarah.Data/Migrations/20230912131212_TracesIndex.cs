using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sarah.Data;

namespace Sarah.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20230912131212_TracesIndex")]
    public partial class TracesIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex("IX_DeviceTraces_CreationDate",
                "DeviceTraces", "CreationDate");
            migrationBuilder.CreateIndex("IX_DeviceTraces_NodeId",
                "DeviceTraces", "NodeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceTraces_CreationDate");
            migrationBuilder.DropIndex(
                name: "IX_DeviceTraces_NodeId");
        }
    }
}
