using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "audit_log",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "audit_log",
                newName: "created_by_person_alias_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "audit_log",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "audit_log",
                newName: "CreatedByPersonAliasId");
        }
    }
}
