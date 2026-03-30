using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFamilyLocationFk : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "location_id",
            table: "family",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_family_location_id",
            table: "family",
            column: "location_id");

        migrationBuilder.AddForeignKey(
            name: "FK_family_location_location_id",
            table: "family",
            column: "location_id",
            principalTable: "location",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_family_location_location_id",
            table: "family");

        migrationBuilder.DropIndex(
            name: "IX_family_location_id",
            table: "family");

        migrationBuilder.DropColumn(
            name: "location_id",
            table: "family");
    }
}
