using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddMissingIndexesForJan2026Tables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Index for export_job pagination ORDER BY created_date_time
        migrationBuilder.CreateIndex(
            name: "ix_export_job_created_date_time",
            table: "export_job",
            column: "created_date_time");

        // Index for user_session filtering by is_active
        migrationBuilder.CreateIndex(
            name: "ix_user_session_is_active",
            table: "user_session",
            column: "is_active");

        // Composite index for user_session common query (person_id, is_active)
        migrationBuilder.CreateIndex(
            name: "ix_user_session_person_id_is_active",
            table: "user_session",
            columns: new[] { "person_id", "is_active" });

        // Index for notification pagination ORDER BY created_date_time
        migrationBuilder.CreateIndex(
            name: "ix_notification_created_date_time",
            table: "notification",
            column: "created_date_time");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_export_job_created_date_time",
            table: "export_job");

        migrationBuilder.DropIndex(
            name: "ix_user_session_is_active",
            table: "user_session");

        migrationBuilder.DropIndex(
            name: "ix_user_session_person_id_is_active",
            table: "user_session");

        migrationBuilder.DropIndex(
            name: "ix_notification_created_date_time",
            table: "notification");
    }
}
