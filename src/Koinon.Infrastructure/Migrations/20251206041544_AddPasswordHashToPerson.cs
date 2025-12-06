using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPasswordHashToPerson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "password_hash",
            table: "person",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "refresh_token",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                replaced_by_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                created_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                revoked_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_refresh_token", x => x.id);
                table.ForeignKey(
                    name: "FK_refresh_token_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_refresh_token_expires_at",
            table: "refresh_token",
            column: "expires_at");

        migrationBuilder.CreateIndex(
            name: "IX_refresh_token_person_id",
            table: "refresh_token",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "uix_refresh_token_guid",
            table: "refresh_token",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_refresh_token_token",
            table: "refresh_token",
            column: "token",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_attendance_person_alias_person_alias_id",
            table: "attendance",
            column: "person_alias_id",
            principalTable: "person_alias",
            principalColumn: "id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_attendance_person_alias_person_alias_id",
            table: "attendance");

        migrationBuilder.DropTable(
            name: "refresh_token");

        migrationBuilder.DropColumn(
            name: "password_hash",
            table: "person");
    }
}
