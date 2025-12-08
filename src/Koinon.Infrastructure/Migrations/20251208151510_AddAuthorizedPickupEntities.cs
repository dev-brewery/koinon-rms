using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAuthorizedPickupEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "authorized_pickup",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                child_person_id = table.Column<int>(type: "integer", nullable: false),
                authorized_person_id = table.Column<int>(type: "integer", nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                relationship = table.Column<int>(type: "integer", nullable: false, defaultValue: 7),
                authorization_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                custody_notes = table.Column<string>(type: "text", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_authorized_pickup", x => x.id);
                table.ForeignKey(
                    name: "FK_authorized_pickup_person_authorized_person_id",
                    column: x => x.authorized_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_authorized_pickup_person_child_person_id",
                    column: x => x.child_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "pickup_log",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                attendance_id = table.Column<int>(type: "integer", nullable: false),
                child_person_id = table.Column<int>(type: "integer", nullable: false),
                pickup_person_id = table.Column<int>(type: "integer", nullable: true),
                pickup_person_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                was_authorized = table.Column<bool>(type: "boolean", nullable: false),
                authorized_pickup_id = table.Column<int>(type: "integer", nullable: true),
                supervisor_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                supervisor_person_id = table.Column<int>(type: "integer", nullable: true),
                checkout_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                notes = table.Column<string>(type: "text", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_pickup_log", x => x.id);
                table.ForeignKey(
                    name: "FK_pickup_log_attendance_attendance_id",
                    column: x => x.attendance_id,
                    principalTable: "attendance",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_pickup_log_authorized_pickup_authorized_pickup_id",
                    column: x => x.authorized_pickup_id,
                    principalTable: "authorized_pickup",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_pickup_log_person_child_person_id",
                    column: x => x.child_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_pickup_log_person_pickup_person_id",
                    column: x => x.pickup_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_pickup_log_person_supervisor_person_id",
                    column: x => x.supervisor_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_authorized_pickup_authorized_person_id",
            table: "authorized_pickup",
            column: "authorized_person_id",
            filter: "authorized_person_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_authorized_pickup_child_person_id",
            table: "authorized_pickup",
            column: "child_person_id");

        migrationBuilder.CreateIndex(
            name: "ix_authorized_pickup_child_person_id_authorization_level",
            table: "authorized_pickup",
            columns: new[] { "child_person_id", "authorization_level" });

        migrationBuilder.CreateIndex(
            name: "uix_authorized_pickup_guid",
            table: "authorized_pickup",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_pickup_log_attendance_id",
            table: "pickup_log",
            column: "attendance_id");

        migrationBuilder.CreateIndex(
            name: "ix_pickup_log_authorized_pickup_id",
            table: "pickup_log",
            column: "authorized_pickup_id");

        migrationBuilder.CreateIndex(
            name: "ix_pickup_log_checkout_date_time",
            table: "pickup_log",
            column: "checkout_date_time");

        migrationBuilder.CreateIndex(
            name: "ix_pickup_log_child_person_id",
            table: "pickup_log",
            column: "child_person_id");

        migrationBuilder.CreateIndex(
            name: "ix_pickup_log_pickup_person_id",
            table: "pickup_log",
            column: "pickup_person_id");

        migrationBuilder.CreateIndex(
            name: "ix_pickup_log_supervisor_person_id",
            table: "pickup_log",
            column: "supervisor_person_id");

        migrationBuilder.CreateIndex(
            name: "uix_pickup_log_guid",
            table: "pickup_log",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "pickup_log");

        migrationBuilder.DropTable(
            name: "authorized_pickup");
    }
}
