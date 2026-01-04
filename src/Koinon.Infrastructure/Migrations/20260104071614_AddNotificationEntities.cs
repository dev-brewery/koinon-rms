using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddNotificationEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notification",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                notification_type = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                read_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                action_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                metadata_json = table.Column<string>(type: "text", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification", x => x.id);
                table.ForeignKey(
                    name: "FK_notification_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "notification_preference",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                notification_type = table.Column<int>(type: "integer", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_preference", x => x.id);
                table.ForeignKey(
                    name: "FK_notification_preference_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_notification_person_id",
            table: "notification",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_notification_person_is_read",
            table: "notification",
            columns: new[] { "person_id", "is_read" });

        migrationBuilder.CreateIndex(
            name: "uix_notification_guid",
            table: "notification",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_notification_preference_person_id",
            table: "notification_preference",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "uix_notification_preference_guid",
            table: "notification_preference",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_notification_preference_person_type",
            table: "notification_preference",
            columns: new[] { "person_id", "notification_type" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notification");

        migrationBuilder.DropTable(
            name: "notification_preference");
    }
}
