using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGroupAgeGradeFiltering : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "max_age_months",
            table: "group",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "max_grade",
            table: "group",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "min_age_months",
            table: "group",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "min_grade",
            table: "group",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "max_age_months",
            table: "group");

        migrationBuilder.DropColumn(
            name: "max_grade",
            table: "group");

        migrationBuilder.DropColumn(
            name: "min_age_months",
            table: "group");

        migrationBuilder.DropColumn(
            name: "min_grade",
            table: "group");
    }
}
