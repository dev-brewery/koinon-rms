using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPhoneNumberNormalized : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "number_normalized",
            table: "phone_number",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        // Populate number_normalized from existing number column
        // Remove all non-digit characters
        migrationBuilder.Sql(@"
                UPDATE phone_number
                SET number_normalized = REGEXP_REPLACE(number, '[^0-9]', '', 'g')
                WHERE number_normalized = '';
            ");

        migrationBuilder.CreateIndex(
            name: "ix_phone_number_normalized",
            table: "phone_number",
            column: "number_normalized");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_phone_number_normalized",
            table: "phone_number");

        migrationBuilder.DropColumn(
            name: "number_normalized",
            table: "phone_number");
    }
}
