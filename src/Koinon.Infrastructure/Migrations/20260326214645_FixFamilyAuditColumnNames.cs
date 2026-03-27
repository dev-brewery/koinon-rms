using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class FixFamilyAuditColumnNames : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "created_by_person_alias_id",
            table: "family",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "modified_by_person_alias_id",
            table: "family",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "created_by_person_alias_id",
            table: "family_member",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "modified_by_person_alias_id",
            table: "family_member",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "created_by_person_alias_id",
            table: "family");

        migrationBuilder.DropColumn(
            name: "modified_by_person_alias_id",
            table: "family");

        migrationBuilder.DropColumn(
            name: "created_by_person_alias_id",
            table: "family_member");

        migrationBuilder.DropColumn(
            name: "modified_by_person_alias_id",
            table: "family_member");
    }
}
