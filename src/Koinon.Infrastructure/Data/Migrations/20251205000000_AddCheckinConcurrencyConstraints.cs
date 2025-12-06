using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddCheckinConcurrencyConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Add unique constraint for atomic GetOrCreateOccurrence
        // Note: The existing unique constraint includes location_id as well
        // This constraint is specifically for the concurrency helper pattern
        migrationBuilder.CreateIndex(
            name: "uix_occurrence_group_date_schedule",
            table: "attendance_occurrence",
            columns: new[] { "group_id", "occurrence_date", "schedule_id" },
            unique: true);

        // 2. Add index for SearchByCodeAsync performance
        // Optimizes lookups by issue_date and code
        migrationBuilder.CreateIndex(
            name: "ix_attendance_code_issued_date",
            table: "attendance_code",
            columns: new[] { "issue_date", "code" });

        // 3. Add index for attendance queries by date range (descending)
        // Optimizes recent attendance lookups
        migrationBuilder.CreateIndex(
            name: "ix_attendance_start_date_desc",
            table: "attendance",
            column: "start_date_time",
            descending: new[] { true });

        // 4. Filtered index: Only unchecked-out attendances
        // PostgreSQL syntax for filtered index
        migrationBuilder.CreateIndex(
            name: "ix_attendance_active",
            table: "attendance",
            columns: new[] { "occurrence_id", "person_alias_id" },
            filter: "end_date_time IS NULL");

        // 5. Add index for recent check-in lookups
        // Optimizes queries by person and date
        migrationBuilder.CreateIndex(
            name: "ix_attendance_recent",
            table: "attendance",
            columns: new[] { "person_alias_id", "start_date_time" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "uix_occurrence_group_date_schedule",
            table: "attendance_occurrence");

        migrationBuilder.DropIndex(
            name: "ix_attendance_code_issued_date",
            table: "attendance_code");

        migrationBuilder.DropIndex(
            name: "ix_attendance_start_date_desc",
            table: "attendance");

        migrationBuilder.DropIndex(
            name: "ix_attendance_active",
            table: "attendance");

        migrationBuilder.DropIndex(
            name: "ix_attendance_recent",
            table: "attendance");
    }
}
