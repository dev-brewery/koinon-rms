using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
// DESTRUCTIVE-CHANGE-APPROVED: drops shadow FK columns CampusId1/PersonId1
// (created by misconfigured relationships, values backfilled into the real
// columns below) and adds the created_by/modified_by_person_alias_id columns
// that supervisor_audit_log was missing (SupervisorAuditLog inherits them from
// Entity and the configuration maps them, but AddSupervisorMode never created
// them). Reviewed as part of the July 2026 handoff hardening; caught by
// SnakeCaseNamingTests.
public partial class FixShadowFkAndAuditColumnNames : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The shadow columns being dropped were written whenever code added
        // rows through the Campus.Groups / Person.PersonAliases navigations
        // (the relationship was misconfigured with an anonymous WithMany()).
        // Preserve any such values before dropping.
        migrationBuilder.Sql(
            "UPDATE \"group\" SET campus_id = \"CampusId1\" WHERE campus_id IS NULL AND \"CampusId1\" IS NOT NULL;");
        migrationBuilder.Sql(
            "UPDATE person_alias SET person_id = \"PersonId1\" WHERE person_id IS NULL AND \"PersonId1\" IS NOT NULL;");

        migrationBuilder.DropForeignKey(
            name: "FK_group_campus_CampusId1",
            table: "group");

        migrationBuilder.DropForeignKey(
            name: "FK_person_alias_person_PersonId1",
            table: "person_alias");

        // NOTE: The shadow indexes IX_person_alias_PersonId1 and
        // IX_group_CampusId1 were already dropped by migration
        // 20251207224038_StandardizeIndexNaming (raw DROP INDEX IF EXISTS),
        // so we do NOT DropIndex them here — they no longer exist and the
        // apply would fail with 42704. The DropColumn calls below also
        // cascade-drop any index still referencing these columns in Postgres.

        migrationBuilder.DropColumn(
            name: "PersonId1",
            table: "person_alias");

        migrationBuilder.DropColumn(
            name: "CampusId1",
            table: "group");

        // Add the audit columns supervisor_audit_log never had. AddSupervisorMode
        // created the table without them; no PascalCase column ever existed, so this
        // is an AddColumn (the auto-generated RenameColumn was based on a drifted
        // snapshot and failed at apply time with 42703).
        migrationBuilder.AddColumn<int>(
            name: "created_by_person_alias_id",
            table: "supervisor_audit_log",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "modified_by_person_alias_id",
            table: "supervisor_audit_log",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "created_by_person_alias_id",
            table: "supervisor_audit_log");

        migrationBuilder.DropColumn(
            name: "modified_by_person_alias_id",
            table: "supervisor_audit_log");

        migrationBuilder.AddColumn<int>(
            name: "PersonId1",
            table: "person_alias",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CampusId1",
            table: "group",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_person_alias_PersonId1",
            table: "person_alias",
            column: "PersonId1");

        migrationBuilder.CreateIndex(
            name: "IX_group_CampusId1",
            table: "group",
            column: "CampusId1");

        migrationBuilder.AddForeignKey(
            name: "FK_group_campus_CampusId1",
            table: "group",
            column: "CampusId1",
            principalTable: "campus",
            principalColumn: "id");

        migrationBuilder.AddForeignKey(
            name: "FK_person_alias_person_PersonId1",
            table: "person_alias",
            column: "PersonId1",
            principalTable: "person",
            principalColumn: "id");
    }
}
