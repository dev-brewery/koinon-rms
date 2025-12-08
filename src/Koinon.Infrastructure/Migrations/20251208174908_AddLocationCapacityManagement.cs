using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddLocationCapacityManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "auto_assign_overflow",
            table: "location",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "overflow_location_id",
            table: "location",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "staff_to_child_ratio",
            table: "location",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_location_overflow_location_id",
            table: "location",
            column: "overflow_location_id");

        migrationBuilder.AddForeignKey(
            name: "fk_location_location_overflow_location_id",
            table: "location",
            column: "overflow_location_id",
            principalTable: "location",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_location_location_overflow_location_id",
            table: "location");

        migrationBuilder.DropIndex(
            name: "ix_location_overflow_location_id",
            table: "location");

        migrationBuilder.DropColumn(
            name: "auto_assign_overflow",
            table: "location");

        migrationBuilder.DropColumn(
            name: "overflow_location_id",
            table: "location");

        migrationBuilder.DropColumn(
            name: "staff_to_child_ratio",
            table: "location");
    }
}
