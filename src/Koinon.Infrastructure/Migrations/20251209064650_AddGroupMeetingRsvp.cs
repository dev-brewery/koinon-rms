using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGroupMeetingRsvp : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "group_meeting_rsvp",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                group_id = table.Column<int>(type: "integer", nullable: false),
                meeting_date = table.Column<DateOnly>(type: "date", nullable: false),
                person_id = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                responded_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group_meeting_rsvp", x => x.id);
                table.ForeignKey(
                    name: "FK_group_meeting_rsvp_group_group_id",
                    column: x => x.group_id,
                    principalTable: "group",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_group_meeting_rsvp_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_group_meeting_rsvp_group_id_meeting_date",
            table: "group_meeting_rsvp",
            columns: new[] { "group_id", "meeting_date" });

        migrationBuilder.CreateIndex(
            name: "ix_group_meeting_rsvp_meeting_date",
            table: "group_meeting_rsvp",
            column: "meeting_date");

        migrationBuilder.CreateIndex(
            name: "ix_group_meeting_rsvp_person_id",
            table: "group_meeting_rsvp",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "uix_group_meeting_rsvp_group_meeting_person",
            table: "group_meeting_rsvp",
            columns: new[] { "group_id", "meeting_date", "person_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_group_meeting_rsvp_guid",
            table: "group_meeting_rsvp",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "group_meeting_rsvp");
    }
}
