using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddContributionEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "contribution",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_alias_id = table.Column<int>(type: "integer", nullable: true),
                batch_id = table.Column<int>(type: "integer", nullable: true),
                transaction_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                transaction_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                transaction_type_value_id = table.Column<int>(type: "integer", nullable: false),
                source_type_value_id = table.Column<int>(type: "integer", nullable: false),
                summary = table.Column<string>(type: "TEXT", nullable: true),
                campus_id = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contribution", x => x.id);
                table.ForeignKey(
                    name: "fk_contribution_campus",
                    column: x => x.campus_id,
                    principalTable: "campus",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_contribution_person_alias",
                    column: x => x.person_alias_id,
                    principalTable: "person_alias",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_contribution_source_type",
                    column: x => x.source_type_value_id,
                    principalTable: "defined_value",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_contribution_transaction_type",
                    column: x => x.transaction_type_value_id,
                    principalTable: "defined_value",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "contribution_detail",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                contribution_id = table.Column<int>(type: "integer", nullable: false),
                fund_id = table.Column<int>(type: "integer", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                summary = table.Column<string>(type: "TEXT", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contribution_detail", x => x.id);
                table.ForeignKey(
                    name: "fk_contribution_detail_contribution",
                    column: x => x.contribution_id,
                    principalTable: "contribution",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_contribution_detail_fund",
                    column: x => x.fund_id,
                    principalTable: "fund",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_contribution_batch",
            table: "contribution",
            column: "batch_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_campus",
            table: "contribution",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_guid",
            table: "contribution",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_contribution_person_alias",
            table: "contribution",
            column: "person_alias_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_source_type",
            table: "contribution",
            column: "source_type_value_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_transaction_date",
            table: "contribution",
            column: "transaction_date_time");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_transaction_type",
            table: "contribution",
            column: "transaction_type_value_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_detail_contribution",
            table: "contribution_detail",
            column: "contribution_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_detail_fund",
            table: "contribution_detail",
            column: "fund_id");

        migrationBuilder.CreateIndex(
            name: "ix_contribution_detail_guid",
            table: "contribution_detail",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "contribution_detail");

        migrationBuilder.DropTable(
            name: "contribution");
    }
}
