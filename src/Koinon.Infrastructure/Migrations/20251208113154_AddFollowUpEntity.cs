using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFollowUpEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "follow_up",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                attendance_id = table.Column<int>(type: "integer", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                notes = table.Column<string>(type: "text", nullable: true),
                assigned_to_person_id = table.Column<int>(type: "integer", nullable: true),
                contacted_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_follow_up", x => x.id);
                table.ForeignKey(
                    name: "FK_follow_up_attendance_attendance_id",
                    column: x => x.attendance_id,
                    principalTable: "attendance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_follow_up_person_assigned_to_person_id",
                    column: x => x.assigned_to_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_follow_up_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_follow_up_assigned_to_person_id",
            table: "follow_up",
            column: "assigned_to_person_id",
            filter: "assigned_to_person_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_follow_up_assigned_to_person_id_status",
            table: "follow_up",
            columns: new[] { "assigned_to_person_id", "status" },
            filter: "assigned_to_person_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_follow_up_attendance_id",
            table: "follow_up",
            column: "attendance_id",
            filter: "attendance_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_follow_up_person_id",
            table: "follow_up",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_follow_up_status",
            table: "follow_up",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "uix_follow_up_guid",
            table: "follow_up",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "follow_up");
    }
}
