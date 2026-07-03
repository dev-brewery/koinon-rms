using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    // DESTRUCTIVE-CHANGE-APPROVED: drops shadow FK columns CampusId1/PersonId1
    // (created by misconfigured relationships, values backfilled into the real
    // columns below) and renames two unmapped PascalCase audit columns on
    // supervisor_audit_log to snake_case. Reviewed as part of the July 2026
    // handoff hardening; caught by SnakeCaseNamingTests.
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

            migrationBuilder.DropIndex(
                name: "IX_person_alias_PersonId1",
                table: "person_alias");

            migrationBuilder.DropIndex(
                name: "IX_group_CampusId1",
                table: "group");

            migrationBuilder.DropColumn(
                name: "PersonId1",
                table: "person_alias");

            migrationBuilder.DropColumn(
                name: "CampusId1",
                table: "group");

            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "supervisor_audit_log",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "supervisor_audit_log",
                newName: "created_by_person_alias_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "supervisor_audit_log",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "supervisor_audit_log",
                newName: "CreatedByPersonAliasId");

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
}
