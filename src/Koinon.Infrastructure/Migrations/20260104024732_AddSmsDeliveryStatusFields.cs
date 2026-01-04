using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSmsDeliveryStatusFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "error_code",
            table: "communication_recipient",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "external_message_id",
            table: "communication_recipient",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_communication_recipient_external_message_id",
            table: "communication_recipient",
            column: "external_message_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_communication_recipient_external_message_id",
            table: "communication_recipient");

        migrationBuilder.DropColumn(
            name: "error_code",
            table: "communication_recipient");

        migrationBuilder.DropColumn(
            name: "external_message_id",
            table: "communication_recipient");
    }
}
