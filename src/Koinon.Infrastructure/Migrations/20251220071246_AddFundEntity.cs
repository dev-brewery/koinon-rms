using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFundEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "fund",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                public_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                description = table.Column<string>(type: "TEXT", nullable: true),
                gl_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                is_tax_deductible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                parent_fund_id = table.Column<int>(type: "integer", nullable: true),
                campus_id = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fund", x => x.id);
                table.ForeignKey(
                    name: "fk_fund_campus",
                    column: x => x.campus_id,
                    principalTable: "campus",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_fund_parent_fund",
                    column: x => x.parent_fund_id,
                    principalTable: "fund",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_fund_active",
            table: "fund",
            column: "is_active",
            filter: "is_active = true");

        migrationBuilder.CreateIndex(
            name: "ix_fund_campus",
            table: "fund",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_fund_guid",
            table: "fund",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_fund_parent",
            table: "fund",
            column: "parent_fund_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "fund");
    }
}
