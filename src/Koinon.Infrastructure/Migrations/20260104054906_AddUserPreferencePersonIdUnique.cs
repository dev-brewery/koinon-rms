using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddUserPreferencePersonIdUnique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_user_preference_person_id",
            table: "user_preference");

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

        migrationBuilder.CreateIndex(
            name: "ix_user_preference_person_id",
            table: "user_preference",
            column: "person_id");
    }
}
