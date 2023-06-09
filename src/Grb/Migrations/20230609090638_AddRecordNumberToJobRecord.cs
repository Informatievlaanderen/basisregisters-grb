using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grb.Migrations
{
    public partial class AddRecordNumberToJobRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecordNumber",
                schema: "BuildingRegistryGrb",
                table: "JobRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Manually added
            migrationBuilder.AddColumn<int>(
                name: "RecordNumber",
                schema: "BuildingRegistryGrb",
                table: "JobRecordsArchive",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordNumber",
                schema: "BuildingRegistryGrb",
                table: "JobRecords");

            // Manually added
            migrationBuilder.DropColumn(
                name: "RecordNumber",
                schema: "BuildingRegistryGrb",
                table: "JobRecordsArchive");
        }
    }
}
