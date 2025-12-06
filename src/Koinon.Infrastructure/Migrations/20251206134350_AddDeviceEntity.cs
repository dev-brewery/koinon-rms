using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    device_type_value_id = table.Column<int>(type: "integer", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    printer_settings = table.Column<string>(type: "jsonb", nullable: true),
                    campus_id = table.Column<int>(type: "integer", nullable: true),
                    locations = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    kiosk_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    kiosk_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device", x => x.id);
                    table.ForeignKey(
                        name: "FK_device_campus_campus_id",
                        column: x => x.campus_id,
                        principalTable: "campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_defined_value_device_type_value_id",
                        column: x => x.device_type_value_id,
                        principalTable: "defined_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_device_id",
                table: "attendance",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_campus_id",
                table: "device",
                column: "campus_id",
                filter: "campus_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_device_device_type_value_id",
                table: "device",
                column: "device_type_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_is_active",
                table: "device",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_device_name",
                table: "device",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "uix_device_guid",
                table: "device",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_device_kiosk_token",
                table: "device",
                column: "kiosk_token",
                unique: true,
                filter: "kiosk_token IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_device_device_id",
                table: "attendance",
                column: "device_id",
                principalTable: "device",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendance_device_device_id",
                table: "attendance");

            migrationBuilder.DropTable(
                name: "device");

            migrationBuilder.DropIndex(
                name: "IX_attendance_device_id",
                table: "attendance");
        }
    }
}
