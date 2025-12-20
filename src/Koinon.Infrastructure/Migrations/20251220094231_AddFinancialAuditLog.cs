using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFinancialAuditLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "financial_audit_log",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                action_type = table.Column<int>(type: "integer", nullable: false),
                entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                entity_id_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                details = table.Column<string>(type: "jsonb", nullable: true),
                timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_financial_audit_log", x => x.id);
                table.ForeignKey(
                    name: "FK_financial_audit_log_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_financial_audit_action",
            table: "financial_audit_log",
            column: "action_type");

        migrationBuilder.CreateIndex(
            name: "ix_financial_audit_entity",
            table: "financial_audit_log",
            columns: new[] { "entity_type", "entity_id_key" });

        migrationBuilder.CreateIndex(
            name: "ix_financial_audit_person",
            table: "financial_audit_log",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_financial_audit_timestamp",
            table: "financial_audit_log",
            column: "timestamp");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "financial_audit_log");
    }
}
