using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCampusToLocation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "campus_id",
            table: "location",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_location_campus_id",
            table: "location",
            column: "campus_id");

        migrationBuilder.AddForeignKey(
            name: "FK_location_campus_campus_id",
            table: "location",
            column: "campus_id",
            principalTable: "campus",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_location_campus_campus_id",
            table: "location");

        migrationBuilder.DropIndex(
            name: "ix_location_campus_id",
            table: "location");

        migrationBuilder.DropColumn(
            name: "campus_id",
            table: "location");
    }
}
