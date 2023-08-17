using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grb.Migrations
{
    public partial class AddErrorCodeToJobRecordArchive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                schema: "BuildingRegistryGrb",
                table: "JobRecordsArchive",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorCode",
                schema: "BuildingRegistryGrb",
                table: "JobRecordsArchive");
        }
    }
}
