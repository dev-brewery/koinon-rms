using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonMergeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "person_merge_history",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "person_merge_history",
                newName: "created_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "person_duplicate_ignore",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "person_duplicate_ignore",
                newName: "created_by_person_alias_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "person_merge_history",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "person_merge_history",
                newName: "CreatedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "person_duplicate_ignore",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "person_duplicate_ignore",
                newName: "CreatedByPersonAliasId");
        }
    }
}
