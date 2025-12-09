using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddVolunteerScheduleAssignment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "volunteer_schedule_assignment",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                group_member_id = table.Column<int>(type: "integer", nullable: false),
                schedule_id = table.Column<int>(type: "integer", nullable: false),
                assigned_date = table.Column<DateOnly>(type: "date", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                decline_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                responded_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_volunteer_schedule_assignment", x => x.id);
                table.ForeignKey(
                    name: "FK_volunteer_schedule_assignment_group_member_group_member_id",
                    column: x => x.group_member_id,
                    principalTable: "group_member",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_volunteer_schedule_assignment_schedule_schedule_id",
                    column: x => x.schedule_id,
                    principalTable: "schedule",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_volunteer_schedule_assignment_assigned_date",
            table: "volunteer_schedule_assignment",
            column: "assigned_date");

        migrationBuilder.CreateIndex(
            name: "ix_volunteer_schedule_assignment_group_member_id",
            table: "volunteer_schedule_assignment",
            column: "group_member_id");

        migrationBuilder.CreateIndex(
            name: "ix_volunteer_schedule_assignment_schedule_id",
            table: "volunteer_schedule_assignment",
            column: "schedule_id");

        migrationBuilder.CreateIndex(
            name: "ix_volunteer_schedule_assignment_status",
            table: "volunteer_schedule_assignment",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "uix_volunteer_schedule_assignment_guid",
            table: "volunteer_schedule_assignment",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_volunteer_schedule_assignment_member_schedule_date",
            table: "volunteer_schedule_assignment",
            columns: new[] { "group_member_id", "schedule_id", "assigned_date" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "volunteer_schedule_assignment");
    }
}
