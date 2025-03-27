using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grb.Migrations
{
    /// <inheritdoc />
    public partial class AddDuplicateRecordIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_JobRecords_GrbObject_Idn_IdnVersion_EventType_GrId",
                schema: "BuildingRegistryGrb",
                table: "JobRecords",
                columns: new[] { "GrbObject", "Idn", "IdnVersion", "EventType", "GrId" },
                filter: "[EventType] = 1 AND [GrId] = -9");

            migrationBuilder.CreateIndex(
                name: "IX_JobRecordsArchive_GrbObject_Idn_IdnVersion_EventType_GrId",
                schema: "BuildingRegistryGrb",
                table: "JobRecordsArchive",
                columns: new[] { "GrbObject", "Idn", "IdnVersion", "EventType", "GrId" },
                filter: "[EventType] = 1 AND [GrId] = -9");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobRecords_GrbObject_Idn_IdnVersion_EventType_GrId",
                schema: "BuildingRegistryGrb",
                table: "JobRecords");

            migrationBuilder.DropIndex(
                name: "IX_JobRecordsArchive_GrbObject_Idn_IdnVersion_EventType_GrId",
                schema: "BuildingRegistryGrb",
                table: "JobRecordsArchive");
        }
    }
}
