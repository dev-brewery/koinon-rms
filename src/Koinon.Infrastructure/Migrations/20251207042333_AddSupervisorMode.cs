using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSupervisorMode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "supervisor_pin_hash",
            table: "person",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "supervisor_session",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_supervisor_session", x => x.id);
                table.ForeignKey(
                    name: "FK_supervisor_session_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_supervisor_session_expires_at",
            table: "supervisor_session",
            column: "expires_at");

        migrationBuilder.CreateIndex(
            name: "ix_supervisor_session_person_id",
            table: "supervisor_session",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "uix_supervisor_session_guid",
            table: "supervisor_session",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_supervisor_session_token",
            table: "supervisor_session",
            column: "token",
            unique: true);

        migrationBuilder.CreateTable(
            name: "supervisor_audit_log",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                person_id = table.Column<int>(type: "integer", nullable: true),
                supervisor_session_id = table.Column<int>(type: "integer", nullable: true),
                action_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                entity_id_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                success = table.Column<bool>(type: "boolean", nullable: false),
                details = table.Column<string>(type: "text", nullable: true),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_supervisor_audit_log", x => x.id);
                table.ForeignKey(
                    name: "FK_supervisor_audit_log_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_supervisor_audit_log_supervisor_session_supervisor_session_id",
                    column: x => x.supervisor_session_id,
                    principalTable: "supervisor_session",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_supervisor_audit_log_guid",
            table: "supervisor_audit_log",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_supervisor_audit_log_action_type",
            table: "supervisor_audit_log",
            column: "action_type");

        migrationBuilder.CreateIndex(
            name: "ix_supervisor_audit_log_person_id_action_type",
            table: "supervisor_audit_log",
            columns: new[] { "person_id", "action_type" });

        migrationBuilder.CreateIndex(
            name: "ix_supervisor_audit_log_created_date_time",
            table: "supervisor_audit_log",
            column: "created_date_time");

        migrationBuilder.CreateIndex(
            name: "ix_supervisor_audit_log_supervisor_session_id",
            table: "supervisor_audit_log",
            column: "supervisor_session_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "supervisor_audit_log");

        migrationBuilder.DropTable(
            name: "supervisor_session");

        migrationBuilder.DropColumn(
            name: "supervisor_pin_hash",
            table: "person");
    }
}
