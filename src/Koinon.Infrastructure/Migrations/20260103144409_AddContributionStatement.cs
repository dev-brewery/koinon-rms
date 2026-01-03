using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddContributionStatement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "contribution_statement",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                contribution_count = table.Column<int>(type: "integer", nullable: false),
                generated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                binary_file_id = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contribution_statement", x => x.id);
                table.ForeignKey(
                    name: "fk_contribution_statement_binary_file",
                    column: x => x.binary_file_id,
                    principalTable: "binary_file",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_contribution_statement_person",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_contribution_statement_binary_file_id",
            table: "contribution_statement",
            column: "binary_file_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_statement_guid",
            table: "contribution_statement",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_contribution_statement_person",
            table: "contribution_statement",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_statement_person_period",
            table: "contribution_statement",
            columns: new[] { "person_id", "start_date", "end_date" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "contribution_statement");
    }
}
