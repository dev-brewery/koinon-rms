using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class StandardizeIndexNaming : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop spurious shadow property indexes
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_group_CampusId1\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_person_alias_PersonId1\";");

        // Rename indexes from PascalCase to snake_case
        migrationBuilder.RenameIndex(
            name: "IX_refresh_token_person_id",
            table: "refresh_token",
            newName: "ix_refresh_token_person_id");

        migrationBuilder.RenameIndex(
            name: "IX_phone_number_number_type_value_id",
            table: "phone_number",
            newName: "ix_phone_number_number_type_value_id");

        migrationBuilder.RenameIndex(
            name: "IX_person_connection_status_value_id",
            table: "person",
            newName: "ix_person_connection_status_value_id");

        migrationBuilder.RenameIndex(
            name: "IX_group_schedule_id",
            table: "group",
            newName: "ix_group_schedule_id");

        migrationBuilder.RenameIndex(
            name: "IX_device_device_type_value_id",
            table: "device",
            newName: "ix_device_device_type_value_id");

        migrationBuilder.RenameIndex(
            name: "IX_campus_campus_status_value_id",
            table: "campus",
            newName: "ix_campus_campus_status_value_id");

        migrationBuilder.RenameIndex(
            name: "IX_attendance_occurrence_schedule_id",
            table: "attendance_occurrence",
            newName: "ix_attendance_occurrence_schedule_id");

        migrationBuilder.RenameIndex(
            name: "IX_attendance_occurrence_location_id",
            table: "attendance_occurrence",
            newName: "ix_attendance_occurrence_location_id");

        migrationBuilder.RenameIndex(
            name: "IX_attendance_device_id",
            table: "attendance",
            newName: "ix_attendance_device_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse the renames (PascalCase indexes are recreated by reverse rename)
        migrationBuilder.RenameIndex(
            name: "ix_refresh_token_person_id",
            table: "refresh_token",
            newName: "IX_refresh_token_person_id");

        migrationBuilder.RenameIndex(
            name: "ix_phone_number_number_type_value_id",
            table: "phone_number",
            newName: "IX_phone_number_number_type_value_id");

        migrationBuilder.RenameIndex(
            name: "ix_person_connection_status_value_id",
            table: "person",
            newName: "IX_person_connection_status_value_id");

        migrationBuilder.RenameIndex(
            name: "ix_group_schedule_id",
            table: "group",
            newName: "IX_group_schedule_id");

        migrationBuilder.RenameIndex(
            name: "ix_device_device_type_value_id",
            table: "device",
            newName: "IX_device_device_type_value_id");

        migrationBuilder.RenameIndex(
            name: "ix_campus_campus_status_value_id",
            table: "campus",
            newName: "IX_campus_campus_status_value_id");

        migrationBuilder.RenameIndex(
            name: "ix_attendance_occurrence_schedule_id",
            table: "attendance_occurrence",
            newName: "IX_attendance_occurrence_schedule_id");

        migrationBuilder.RenameIndex(
            name: "ix_attendance_occurrence_location_id",
            table: "attendance_occurrence",
            newName: "IX_attendance_occurrence_location_id");

        migrationBuilder.RenameIndex(
            name: "ix_attendance_device_id",
            table: "attendance",
            newName: "IX_attendance_device_id");
    }
}
