using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "report_schedule",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "report_schedule",
                newName: "created_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "report_run",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "report_run",
                newName: "created_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "report_definition",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "report_definition",
                newName: "created_by_person_alias_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "report_schedule",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "report_schedule",
                newName: "CreatedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "report_run",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "report_run",
                newName: "CreatedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "report_definition",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "report_definition",
                newName: "CreatedByPersonAliasId");
        }
    }
}
