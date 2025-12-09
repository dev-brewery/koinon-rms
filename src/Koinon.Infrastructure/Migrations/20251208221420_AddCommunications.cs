using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCommunications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "communication",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                communication_type = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                body = table.Column<string>(type: "text", nullable: false),
                from_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                from_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                reply_to_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                sent_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                recipient_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                delivered_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                failed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                opened_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_communication", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "communication_recipient",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                communication_id = table.Column<int>(type: "integer", nullable: false),
                person_id = table.Column<int>(type: "integer", nullable: false),
                address = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                delivered_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                opened_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                group_id = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_communication_recipient", x => x.id);
                table.ForeignKey(
                    name: "FK_communication_recipient_communication_communication_id",
                    column: x => x.communication_id,
                    principalTable: "communication",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_communication_recipient_group_group_id",
                    column: x => x.group_id,
                    principalTable: "group",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_communication_recipient_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_communication_communication_type",
            table: "communication",
            column: "communication_type");

        migrationBuilder.CreateIndex(
            name: "ix_communication_created_date_time",
            table: "communication",
            column: "created_date_time");

        migrationBuilder.CreateIndex(
            name: "ix_communication_sent_date_time",
            table: "communication",
            column: "sent_date_time");

        migrationBuilder.CreateIndex(
            name: "ix_communication_status",
            table: "communication",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "uix_communication_guid",
            table: "communication",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_communication_recipient_communication_id",
            table: "communication_recipient",
            column: "communication_id");

        migrationBuilder.CreateIndex(
            name: "ix_communication_recipient_group_id",
            table: "communication_recipient",
            column: "group_id");

        migrationBuilder.CreateIndex(
            name: "ix_communication_recipient_person_id",
            table: "communication_recipient",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_communication_recipient_status",
            table: "communication_recipient",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "uix_communication_recipient_guid",
            table: "communication_recipient",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "communication_recipient");

        migrationBuilder.DropTable(
            name: "communication");
    }
}
