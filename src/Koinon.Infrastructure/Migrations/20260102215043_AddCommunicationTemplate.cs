using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCommunicationTemplate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "control_item_count",
            table: "contribution_batch",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "communication_template",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                communication_type = table.Column<int>(type: "integer", nullable: false),
                subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                body = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_communication_template", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_communication_template_communication_type",
            table: "communication_template",
            column: "communication_type");

        migrationBuilder.CreateIndex(
            name: "ix_communication_template_is_active",
            table: "communication_template",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "uix_communication_template_guid",
            table: "communication_template",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "communication_template");

        migrationBuilder.DropColumn(
            name: "control_item_count",
            table: "contribution_batch");
    }
}
