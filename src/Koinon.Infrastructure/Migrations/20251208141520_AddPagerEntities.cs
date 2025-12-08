using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPagerEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "pager_assignment",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                attendance_id = table.Column<int>(type: "integer", nullable: false),
                pager_number = table.Column<int>(type: "integer", nullable: false),
                campus_id = table.Column<int>(type: "integer", nullable: true),
                location_id = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_pager_assignment", x => x.id);
                table.ForeignKey(
                    name: "FK_pager_assignment_attendance_attendance_id",
                    column: x => x.attendance_id,
                    principalTable: "attendance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_pager_assignment_campus_campus_id",
                    column: x => x.campus_id,
                    principalTable: "campus",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_pager_assignment_location_location_id",
                    column: x => x.location_id,
                    principalTable: "location",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "pager_message",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                pager_assignment_id = table.Column<int>(type: "integer", nullable: false),
                sent_by_person_id = table.Column<int>(type: "integer", nullable: false),
                message_type = table.Column<int>(type: "integer", nullable: false),
                message_text = table.Column<string>(type: "text", nullable: false),
                phone_number = table.Column<string>(type: "text", nullable: false),
                twilio_message_sid = table.Column<string>(type: "text", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                sent_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                delivered_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_pager_message", x => x.id);
                table.ForeignKey(
                    name: "FK_pager_message_pager_assignment_pager_assignment_id",
                    column: x => x.pager_assignment_id,
                    principalTable: "pager_assignment",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_pager_message_person_sent_by_person_id",
                    column: x => x.sent_by_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_pager_assignment_campus_id_created_date_time",
            table: "pager_assignment",
            columns: new[] { "campus_id", "created_date_time" });

        migrationBuilder.CreateIndex(
            name: "IX_pager_assignment_location_id",
            table: "pager_assignment",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "ix_pager_assignment_pager_number",
            table: "pager_assignment",
            column: "pager_number");

        migrationBuilder.CreateIndex(
            name: "uix_pager_assignment_attendance_id",
            table: "pager_assignment",
            column: "attendance_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_pager_assignment_guid",
            table: "pager_assignment",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_pager_message_pager_assignment_id",
            table: "pager_message",
            column: "pager_assignment_id");

        migrationBuilder.CreateIndex(
            name: "ix_pager_message_sent_by_person_id",
            table: "pager_message",
            column: "sent_by_person_id");

        migrationBuilder.CreateIndex(
            name: "ix_pager_message_status",
            table: "pager_message",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_pager_message_twilio_message_sid",
            table: "pager_message",
            column: "twilio_message_sid",
            filter: "twilio_message_sid IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "uix_pager_message_guid",
            table: "pager_message",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "pager_message");

        migrationBuilder.DropTable(
            name: "pager_assignment");
    }
}
