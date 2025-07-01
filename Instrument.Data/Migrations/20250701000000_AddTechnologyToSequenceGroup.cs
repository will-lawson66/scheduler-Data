using Microsoft.EntityFrameworkCore.Migrations;

namespace Instrument.Data.Migrations
{
    public partial class AddTechnologyToSequenceGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Technology",
                table: "SequenceGroups",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Technology",
                table: "SequenceGroups");
        }
    }
}