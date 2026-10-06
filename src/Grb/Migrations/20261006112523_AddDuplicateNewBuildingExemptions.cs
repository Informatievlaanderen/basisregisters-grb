using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grb.Migrations
{
    /// <inheritdoc />
    public partial class AddDuplicateNewBuildingExemptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuplicateNewBuildingExemptions",
                schema: "BuildingRegistryGrb",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Idn = table.Column<long>(type: "bigint", nullable: false),
                    IdnVersion = table.Column<int>(type: "int", nullable: false),
                    GrbObject = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateNewBuildingExemptions", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateNewBuildingExemptions_Idn_IdnVersion_GrbObject",
                schema: "BuildingRegistryGrb",
                table: "DuplicateNewBuildingExemptions",
                columns: new[] { "Idn", "IdnVersion", "GrbObject" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuplicateNewBuildingExemptions",
                schema: "BuildingRegistryGrb");
        }
    }
}
