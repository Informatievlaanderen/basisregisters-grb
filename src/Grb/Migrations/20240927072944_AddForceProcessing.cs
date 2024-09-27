using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grb.Migrations
{
    /// <inheritdoc />
    public partial class AddForceProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ForceProcessing",
                schema: "BuildingRegistryGrb",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForceProcessing",
                schema: "BuildingRegistryGrb",
                table: "Jobs");
        }
    }
}
