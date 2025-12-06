using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCheckinEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.CreateTable(
            name: "attendance_code",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                issue_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_attendance_code", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "schedule",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                icalendar_content = table.Column<string>(type: "text", nullable: true),
                check_in_start_offset_minutes = table.Column<int>(type: "integer", nullable: true),
                check_in_end_offset_minutes = table.Column<int>(type: "integer", nullable: true),
                effective_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                effective_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                category_id = table.Column<int>(type: "integer", nullable: true),
                weekly_day_of_week = table.Column<int>(type: "integer", nullable: true),
                weekly_time_of_day = table.Column<TimeSpan>(type: "interval", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                auto_inactivate_when_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_schedule", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "attendance_occurrence",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                group_id = table.Column<int>(type: "integer", nullable: true),
                location_id = table.Column<int>(type: "integer", nullable: true),
                schedule_id = table.Column<int>(type: "integer", nullable: true),
                occurrence_date = table.Column<DateOnly>(type: "date", nullable: false),
                did_not_occur = table.Column<bool>(type: "boolean", nullable: true),
                sunday_date = table.Column<DateOnly>(type: "date", nullable: false),
                notes = table.Column<string>(type: "text", nullable: true),
                anonymous_attendance_count = table.Column<int>(type: "integer", nullable: true),
                attendance_type_value_id = table.Column<int>(type: "integer", nullable: true),
                decline_confirmation_message = table.Column<string>(type: "text", nullable: true),
                show_decline_reasons = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                accept_confirmation_message = table.Column<string>(type: "text", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_attendance_occurrence", x => x.id);
                table.ForeignKey(
                    name: "FK_attendance_occurrence_group_group_id",
                    column: x => x.group_id,
                    principalTable: "group",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_attendance_occurrence_location_location_id",
                    column: x => x.location_id,
                    principalTable: "location",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_attendance_occurrence_schedule_schedule_id",
                    column: x => x.schedule_id,
                    principalTable: "schedule",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "attendance",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                occurrence_id = table.Column<int>(type: "integer", nullable: false),
                person_alias_id = table.Column<int>(type: "integer", nullable: true),
                device_id = table.Column<int>(type: "integer", nullable: true),
                attendance_code_id = table.Column<int>(type: "integer", nullable: true),
                qualifier_value_id = table.Column<int>(type: "integer", nullable: true),
                start_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                rsvp = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                did_attend = table.Column<bool>(type: "boolean", nullable: true),
                note = table.Column<string>(type: "text", nullable: true),
                campus_id = table.Column<int>(type: "integer", nullable: true),
                processed_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_first_time = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                present_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                present_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                checked_out_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                requested_to_attend = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                scheduled_to_attend = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                decline_reason_value_id = table.Column<int>(type: "integer", nullable: true),
                scheduled_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                schedule_confirmation_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_attendance", x => x.id);
                table.ForeignKey(
                    name: "FK_attendance_attendance_code_attendance_code_id",
                    column: x => x.attendance_code_id,
                    principalTable: "attendance_code",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_attendance_attendance_occurrence_occurrence_id",
                    column: x => x.occurrence_id,
                    principalTable: "attendance_occurrence",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_person_alias_PersonId1",
            table: "person_alias",
            column: "PersonId1");

        migrationBuilder.CreateIndex(
            name: "IX_person_connection_status_value_id",
            table: "person",
            column: "connection_status_value_id");

        migrationBuilder.CreateIndex(
            name: "IX_group_CampusId1",
            table: "group",
            column: "CampusId1");

        migrationBuilder.CreateIndex(
            name: "IX_group_schedule_id",
            table: "group",
            column: "schedule_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_code_id",
            table: "attendance",
            column: "attendance_code_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_did_attend",
            table: "attendance",
            column: "did_attend",
            filter: "did_attend = true");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_occurrence_id",
            table: "attendance",
            column: "occurrence_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_person_alias_id",
            table: "attendance",
            column: "person_alias_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_start_date_time",
            table: "attendance",
            column: "start_date_time");

        migrationBuilder.CreateIndex(
            name: "uix_attendance_guid",
            table: "attendance",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_attendance_code_code",
            table: "attendance_code",
            column: "code");

        migrationBuilder.CreateIndex(
            name: "uix_attendance_code_guid",
            table: "attendance_code",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_attendance_code_issue_date_code",
            table: "attendance_code",
            columns: new[] { "issue_date_time", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_attendance_occurrence_date",
            table: "attendance_occurrence",
            column: "occurrence_date");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_occurrence_group_id",
            table: "attendance_occurrence",
            column: "group_id");

        migrationBuilder.CreateIndex(
            name: "IX_attendance_occurrence_location_id",
            table: "attendance_occurrence",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "IX_attendance_occurrence_schedule_id",
            table: "attendance_occurrence",
            column: "schedule_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_occurrence_sunday_date",
            table: "attendance_occurrence",
            column: "sunday_date");

        migrationBuilder.CreateIndex(
            name: "uix_attendance_occurrence_group_location_schedule_date",
            table: "attendance_occurrence",
            columns: new[] { "group_id", "location_id", "schedule_id", "occurrence_date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_attendance_occurrence_guid",
            table: "attendance_occurrence",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_schedule_name",
            table: "schedule",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "ix_schedule_weekly",
            table: "schedule",
            columns: new[] { "weekly_day_of_week", "weekly_time_of_day" });

        migrationBuilder.CreateIndex(
            name: "uix_schedule_guid",
            table: "schedule",
            column: "guid",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_group_campus_CampusId1",
            table: "group",
            column: "CampusId1",
            principalTable: "campus",
            principalColumn: "id");

        migrationBuilder.AddForeignKey(
            name: "FK_group_schedule_schedule_id",
            table: "group",
            column: "schedule_id",
            principalTable: "schedule",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_person_campus_primary_campus_id",
            table: "person",
            column: "primary_campus_id",
            principalTable: "campus",
            principalColumn: "id");

        migrationBuilder.AddForeignKey(
            name: "FK_person_defined_value_connection_status_value_id",
            table: "person",
            column: "connection_status_value_id",
            principalTable: "defined_value",
            principalColumn: "id");

        migrationBuilder.AddForeignKey(
            name: "FK_person_defined_value_record_status_value_id",
            table: "person",
            column: "record_status_value_id",
            principalTable: "defined_value",
            principalColumn: "id");

        migrationBuilder.AddForeignKey(
            name: "FK_person_alias_person_PersonId1",
            table: "person_alias",
            column: "PersonId1",
            principalTable: "person",
            principalColumn: "id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_group_campus_CampusId1",
            table: "group");

        migrationBuilder.DropForeignKey(
            name: "FK_group_schedule_schedule_id",
            table: "group");

        migrationBuilder.DropForeignKey(
            name: "FK_person_campus_primary_campus_id",
            table: "person");

        migrationBuilder.DropForeignKey(
            name: "FK_person_defined_value_connection_status_value_id",
            table: "person");

        migrationBuilder.DropForeignKey(
            name: "FK_person_defined_value_record_status_value_id",
            table: "person");

        migrationBuilder.DropForeignKey(
            name: "FK_person_alias_person_PersonId1",
            table: "person_alias");

        migrationBuilder.DropTable(
            name: "attendance");

        migrationBuilder.DropTable(
            name: "attendance_code");

        migrationBuilder.DropTable(
            name: "attendance_occurrence");

        migrationBuilder.DropTable(
            name: "schedule");

        migrationBuilder.DropIndex(
            name: "IX_person_alias_PersonId1",
            table: "person_alias");

        migrationBuilder.DropIndex(
            name: "IX_person_connection_status_value_id",
            table: "person");

        migrationBuilder.DropIndex(
            name: "IX_group_CampusId1",
            table: "group");

        migrationBuilder.DropIndex(
            name: "IX_group_schedule_id",
            table: "group");

        migrationBuilder.DropColumn(
            name: "PersonId1",
            table: "person_alias");

        migrationBuilder.DropColumn(
            name: "CampusId1",
            table: "group");
    }
}
