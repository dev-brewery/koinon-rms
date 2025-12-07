using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPersonAllergyFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "special_needs",
            table: "person",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "allergies",
            table: "person",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "special_needs",
            table: "person",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(2000)",
            oldMaxLength: 2000,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "allergies",
            table: "person",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(500)",
            oldMaxLength: 500,
            oldNullable: true);
    }
}
