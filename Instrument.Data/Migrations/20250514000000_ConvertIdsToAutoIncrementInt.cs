using Microsoft.EntityFrameworkCore.Migrations;

namespace Instrument.Data.Migrations
{
    public partial class ConvertIdsToAutoIncrementInt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create temporary tables with integer IDs
            
            // ------ Sequence Table ------
            migrationBuilder.CreateTable(
                name: "SequencesTemp",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true), // Use SqlServer:Identity for SQL Server
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    WorstCaseTime = table.Column<TimeSpan>(nullable: false),
                    CanBeParallel = table.Column<bool>(nullable: false)
                });
                
            // Copy data (excluding ID which will be auto-generated)
            migrationBuilder.Sql(@"
                INSERT INTO SequencesTemp (Name, Description, WorstCaseTime, CanBeParallel)
                SELECT Name, Description, WorstCaseTime, CanBeParallel
                FROM Sequences
            ");
            
            // Create a mapping table to keep track of old IDs to new IDs
            migrationBuilder.Sql(@"
                CREATE TABLE SequenceIdMapping (
                    OldId TEXT NOT NULL,
                    NewId INTEGER NOT NULL,
                    Name TEXT NOT NULL
                )
            ");
            
            // Populate the mapping table
            migrationBuilder.Sql(@"
                INSERT INTO SequenceIdMapping (OldId, NewId, Name)
                SELECT s.Id, st.Id, st.Name
                FROM Sequences s
                JOIN SequencesTemp st ON s.Name = st.Name
            ");
            
            // ------ Parameter Table ------
            migrationBuilder.CreateTable(
                name: "ParametersTemp",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Min = table.Column<string>(maxLength: 100, nullable: true),
                    Max = table.Column<string>(maxLength: 100, nullable: true),
                    DefaultValue = table.Column<string>(maxLength: 1000, nullable: true),
                    Format = table.Column<string>(maxLength: 100, nullable: true),
                    RangeId = table.Column<int>(nullable: true),
                    ResourceId = table.Column<int>(nullable: true)
                });
                
            // Copy data (excluding ID which will be auto-generated)
            migrationBuilder.Sql(@"
                INSERT INTO ParametersTemp (Name, Type, Min, Max, DefaultValue, Format)
                SELECT Name, Type, Min, Max, DefaultValue, Format
                FROM Parameters
            ");
            
            // Create Parameter mapping table
            migrationBuilder.Sql(@"
                CREATE TABLE ParameterIdMapping (
                    OldId TEXT NOT NULL,
                    NewId INTEGER NOT NULL,
                    Name TEXT NOT NULL
                )
            ");
            
            // Populate the mapping table
            migrationBuilder.Sql(@"
                INSERT INTO ParameterIdMapping (OldId, NewId, Name)
                SELECT p.Id, pt.Id, pt.Name
                FROM Parameters p
                JOIN ParametersTemp pt ON p.Name = pt.Name
            ");
            
            // ------ Range Table ------
            // Similar pattern for Range table
            // ... (same pattern for other entity tables)

            // Step 2: Create new join tables with integer IDs
            
            // ------ SequenceParameter Join Table ------
            migrationBuilder.CreateTable(
                name: "SequenceParametersTemp",
                columns: table => new
                {
                    SequenceId = table.Column<int>(nullable: false),
                    ParameterId = table.Column<int>(nullable: false),
                    OrderNumber = table.Column<int>(nullable: false)
                });
                
            // Copy data, mapping old string IDs to new integer IDs
            migrationBuilder.Sql(@"
                INSERT INTO SequenceParametersTemp (SequenceId, ParameterId, OrderNumber)
                SELECT 
                    (SELECT NewId FROM SequenceIdMapping WHERE OldId = sp.SequenceId),
                    (SELECT NewId FROM ParameterIdMapping WHERE OldId = sp.ParameterId),
                    sp.OrderNumber
                FROM SequenceParameters sp
            ");

            // Step 3: Drop old tables and rename new ones
            migrationBuilder.DropTable("SequenceParameters");
            migrationBuilder.RenameTable("SequenceParametersTemp", "SequenceParameters");
            
            migrationBuilder.DropTable("Parameters");
            migrationBuilder.RenameTable("ParametersTemp", "Parameters");
            
            migrationBuilder.DropTable("Sequences");
            migrationBuilder.RenameTable("SequencesTemp", "Sequences");
            
            // ... (similar for other tables)

            // Step 4: Add constraints and indexes
            migrationBuilder.AddPrimaryKey(
                name: "PK_SequenceParameters",
                table: "SequenceParameters",
                columns: ["SequenceId", "ParameterId"]);
                
            migrationBuilder.AddForeignKey(
                name: "FK_SequenceParameters_Sequences_SequenceId",
                table: "SequenceParameters",
                column: "SequenceId",
                principalTable: "Sequences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
                
            migrationBuilder.AddForeignKey(
                name: "FK_SequenceParameters_Parameters_ParameterId",
                table: "SequenceParameters",
                column: "ParameterId",
                principalTable: "Parameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
                
            // Clean up the mapping tables as they're no longer needed
            migrationBuilder.Sql("DROP TABLE SequenceIdMapping");
            migrationBuilder.Sql("DROP TABLE ParameterIdMapping");
            // ... (drop other mapping tables)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Going back from integer ID to string ID is complex and risky
            throw new NotSupportedException("Downgrading from integer IDs to string IDs is not supported due to potential data loss.");
        }
    }
}
