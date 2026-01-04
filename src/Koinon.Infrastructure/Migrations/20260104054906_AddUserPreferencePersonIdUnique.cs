using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferencePersonIdUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_preference_person_id",
                table: "user_preference");

            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "user_session",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "user_session",
                newName: "created_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "user_preference",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "user_preference",
                newName: "created_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "ModifiedByPersonAliasId",
                table: "two_factor_config",
                newName: "modified_by_person_alias_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByPersonAliasId",
                table: "two_factor_config",
                newName: "created_by_person_alias_id");

            migrationBuilder.CreateIndex(
                name: "uix_user_preference_person_id",
                table: "user_preference",
                column: "person_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uix_user_preference_person_id",
                table: "user_preference");

            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "user_session",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "user_session",
                newName: "CreatedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "user_preference",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "user_preference",
                newName: "CreatedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "modified_by_person_alias_id",
                table: "two_factor_config",
                newName: "ModifiedByPersonAliasId");

            migrationBuilder.RenameColumn(
                name: "created_by_person_alias_id",
                table: "two_factor_config",
                newName: "CreatedByPersonAliasId");

            migrationBuilder.CreateIndex(
                name: "ix_user_preference_person_id",
                table: "user_preference",
                column: "person_id");
        }
    }
}
