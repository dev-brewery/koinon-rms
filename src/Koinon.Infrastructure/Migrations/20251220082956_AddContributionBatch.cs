using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddContributionBatch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "contribution_batch",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                batch_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                control_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                campus_id = table.Column<int>(type: "integer", nullable: true),
                note = table.Column<string>(type: "TEXT", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contribution_batch", x => x.id);
                table.ForeignKey(
                    name: "fk_contribution_batch_campus",
                    column: x => x.campus_id,
                    principalTable: "campus",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_contribution_batch_batch_date",
            table: "contribution_batch",
            column: "batch_date");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_batch_campus",
            table: "contribution_batch",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_batch_guid",
            table: "contribution_batch",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_contribution_batch_status",
            table: "contribution_batch",
            column: "status");

        migrationBuilder.AddForeignKey(
            name: "fk_contribution_batch",
            table: "contribution",
            column: "batch_id",
            principalTable: "contribution_batch",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_contribution_batch",
            table: "contribution");

        migrationBuilder.DropTable(
            name: "contribution_batch");
    }
}
