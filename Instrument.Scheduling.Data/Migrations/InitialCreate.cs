using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Instrument.Scheduling.Data.Migrations;

public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Sequences",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                Description = table.Column<string>(maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sequences", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Parameters",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Type = table.Column<string>(maxLength: 50, nullable: false),
                DefaultValue = table.Column<string>(maxLength: 1000, nullable: true),
                Min = table.Column<string>(maxLength: 100, nullable: true),
                Max = table.Column<string>(maxLength: 100, nullable: true),
                Format = table.Column<string>(maxLength: 100, nullable: true),
                RangeId = table.Column<string>(maxLength: 50, nullable: true),
                ResourceId = table.Column<string>(maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Parameters", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Ranges",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Description = table.Column<string>(maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ranges", x => x.Id);
            });

        // Add indexes
        migrationBuilder.CreateIndex(
            name: "IX_Sequence_Name",
            table: "Sequences",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Parameter_Name",
            table: "Parameters",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Parameter_Type",
            table: "Parameters",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_Parameter_Range_Resource",
            table: "Parameters",
            columns: new[] { "RangeId", "ResourceId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SequenceParameters");
        migrationBuilder.DropTable(name: "Parameters");
        migrationBuilder.DropTable(name: "Ranges");
        migrationBuilder.DropTable(name: "Sequences");
        migrationBuilder.DropTable(name: "Resources");
    }
}
