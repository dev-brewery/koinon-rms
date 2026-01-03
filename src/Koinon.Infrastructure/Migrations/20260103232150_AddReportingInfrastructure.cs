using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_definition",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    report_type = table.Column<int>(type: "integer", nullable: false),
                    parameter_schema = table.Column<string>(type: "jsonb", nullable: false),
                    default_parameters = table.Column<string>(type: "jsonb", nullable: true),
                    output_format = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_definition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_run",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_definition_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    parameters = table.Column<string>(type: "jsonb", nullable: false),
                    output_file_id = table.Column<int>(type: "integer", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    requested_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_run", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_run_binary_file_output_file_id",
                        column: x => x.output_file_id,
                        principalTable: "binary_file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_report_run_person_alias_requested_by_person_alias_id",
                        column: x => x.requested_by_person_alias_id,
                        principalTable: "person_alias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_report_run_report_definition_report_definition_id",
                        column: x => x.report_definition_id,
                        principalTable: "report_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_definition_id = table.Column<int>(type: "integer", nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "America/New_York"),
                    parameters = table.Column<string>(type: "jsonb", nullable: false),
                    recipient_person_alias_ids = table.Column<string>(type: "jsonb", nullable: false),
                    output_format = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_schedule_report_definition_report_definition_id",
                        column: x => x.report_definition_id,
                        principalTable: "report_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_is_active",
                table: "report_definition",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_report_type",
                table: "report_definition",
                column: "report_type");

            migrationBuilder.CreateIndex(
                name: "uix_report_definition_guid",
                table: "report_definition",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_run_output_file_id",
                table: "report_run",
                column: "output_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_run_report_definition_id_status",
                table: "report_run",
                columns: new[] { "report_definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_report_run_requested_by_person_alias_id",
                table: "report_run",
                column: "requested_by_person_alias_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_run_status",
                table: "report_run",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uix_report_run_guid",
                table: "report_run",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_schedule_is_active",
                table: "report_schedule",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_report_schedule_next_run_at",
                table: "report_schedule",
                column: "next_run_at");

            migrationBuilder.CreateIndex(
                name: "IX_report_schedule_report_definition_id",
                table: "report_schedule",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "uix_report_schedule_guid",
                table: "report_schedule",
                column: "guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_run");

            migrationBuilder.DropTable(
                name: "report_schedule");

            migrationBuilder.DropTable(
                name: "report_definition");
        }
    }
}
