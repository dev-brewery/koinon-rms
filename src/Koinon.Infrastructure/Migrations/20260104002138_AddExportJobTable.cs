using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddExportJobTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "export_job",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                export_type = table.Column<int>(type: "integer", nullable: false),
                entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                parameters = table.Column<string>(type: "jsonb", nullable: false),
                output_format = table.Column<int>(type: "integer", nullable: false),
                output_file_id = table.Column<int>(type: "integer", nullable: true),
                requested_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                record_count = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_export_job", x => x.id);
                table.ForeignKey(
                    name: "FK_export_job_binary_file_output_file_id",
                    column: x => x.output_file_id,
                    principalTable: "binary_file",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_export_job_person_alias_requested_by_person_alias_id",
                    column: x => x.requested_by_person_alias_id,
                    principalTable: "person_alias",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_export_job_export_type",
            table: "export_job",
            column: "export_type");

        migrationBuilder.CreateIndex(
            name: "IX_export_job_output_file_id",
            table: "export_job",
            column: "output_file_id");

        migrationBuilder.CreateIndex(
            name: "IX_export_job_requested_by_person_alias_id",
            table: "export_job",
            column: "requested_by_person_alias_id");

        migrationBuilder.CreateIndex(
            name: "ix_export_job_status",
            table: "export_job",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "uix_export_job_guid",
            table: "export_job",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "export_job");
    }
}
