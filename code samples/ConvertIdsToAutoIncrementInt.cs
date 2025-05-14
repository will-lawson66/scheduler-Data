using Microsoft.EntityFrameworkCore.Migrations;

namespace Instrument.Data.Migrations
{
    public partial class ConvertIdsToAutoIncrementInt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // For each table, we need to:
            // 1. Create a temporary table with the new schema
            // 2. Copy data from the old table to the new one
            // 3. Drop the old table
            // 4. Rename the temporary table to the original name

            // Example for Sequence table
            migrationBuilder.CreateTable(
                name: "SequencesTemp",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true), // SQLite auto-increment
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    WorstCaseTime = table.Column<TimeSpan>(nullable: false),
                    CanBeParallel = table.Column<bool>(nullable: false)
                });

            // Copy data (excluding ID, which will be auto-generated)
            migrationBuilder.Sql(@"
                INSERT INTO SequencesTemp (Name, Description, WorstCaseTime, CanBeParallel)
                SELECT Name, Description, WorstCaseTime, CanBeParallel
                FROM Sequences
            ");

            // Drop original table and rename temp table
            migrationBuilder.DropTable("Sequences");
            migrationBuilder.RenameTable("SequencesTemp", "Sequences");

            // Repeat for all other tables...
            
            // For join tables like SequenceParameter, need to handle relationship between new IDs
            // This is more complex and may require custom SQL to map old string IDs to new int IDs
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // If needed, implement logic to revert to string IDs
            // This would be complex and risk data loss, so typically not implemented
            throw new NotSupportedException("Reverting from integer IDs to string IDs is not supported.");
        }
    }
}