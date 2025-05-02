using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Instrument\.Data.Migrations;

public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // First create the main tables
        migrationBuilder.CreateTable(
            name: "Sequences",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                Description = table.Column<string>(maxLength: 1000, nullable: true),
                WorstCaseTime = table.Column<long>(nullable: false, defaultValue: 30000), // Stored as ticks
                CanBeParallel = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sequences", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Resources",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Type = table.Column<string>(maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Resources", x => x.Id);
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
                table.ForeignKey(
                    name: "FK_Parameters_Ranges_RangeId",
                    column: x => x.RangeId,
                    principalTable: "Ranges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Parameters_Resources_ResourceId",
                    column: x => x.ResourceId,
                    principalTable: "Resources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "RangeValues",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 50, nullable: false),
                RangeId = table.Column<string>(maxLength: 50, nullable: false),
                Value = table.Column<string>(maxLength: 1000, nullable: false),
                DisplayName = table.Column<string>(maxLength: 200, nullable: true),
                SortOrder = table.Column<int>(nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RangeValues", x => new { x.Id, x.RangeId });
                table.ForeignKey(
                    name: "FK_RangeValues_Ranges_RangeId",
                    column: x => x.RangeId,
                    principalTable: "Ranges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SequenceParameters",
            columns: table => new
            {
                SequenceId = table.Column<string>(maxLength: 50, nullable: false),
                ParameterId = table.Column<string>(maxLength: 50, nullable: false),
                OrderNumber = table.Column<int>(nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SequenceParameters", x => new { x.SequenceId, x.ParameterId });
                table.ForeignKey(
                    name: "FK_SequenceParameters_Sequences_SequenceId",
                    column: x => x.SequenceId,
                    principalTable: "Sequences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SequenceParameters_Parameters_ParameterId",
                    column: x => x.ParameterId,
                    principalTable: "Parameters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Add indexes
        migrationBuilder.CreateIndex(
            name: "IX_Sequence_Name",
            table: "Sequences",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Resource_Name",
            table: "Resources",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Range_Name",
            table: "Ranges",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_RangeValue_Range",
            table: "RangeValues",
            column: "RangeId");

        migrationBuilder.CreateIndex(
            name: "IX_Parameter_Name",
            table: "Parameters",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Parameter_Type",
            table: "Parameters",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_Parameter_Range",
            table: "Parameters",
            column: "RangeId");

        migrationBuilder.CreateIndex(
            name: "IX_Parameter_Resource",
            table: "Parameters",
            column: "ResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_SequenceParameter_Parameter",
            table: "SequenceParameters",
            column: "ParameterId");
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
