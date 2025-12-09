using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGroupTypeConfigurationFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "allow_self_registration",
            table: "group_type",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "color",
            table: "group_type",
            type: "character varying(7)",
            maxLength: 7,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "default_group_capacity",
            table: "group_type",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "default_is_public",
            table: "group_type",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "requires_member_approval",
            table: "group_type",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "group_member_request",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                group_id = table.Column<int>(type: "integer", nullable: false),
                person_id = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                request_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                response_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                processed_by_person_id = table.Column<int>(type: "integer", nullable: true),
                processed_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group_member_request", x => x.id);
                table.ForeignKey(
                    name: "FK_group_member_request_group_group_id",
                    column: x => x.group_id,
                    principalTable: "group",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_group_member_request_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_group_member_request_person_processed_by_person_id",
                    column: x => x.processed_by_person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_group_member_request_group_id_status",
            table: "group_member_request",
            columns: new[] { "group_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_group_member_request_person_id",
            table: "group_member_request",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "IX_group_member_request_processed_by_person_id",
            table: "group_member_request",
            column: "processed_by_person_id");

        migrationBuilder.CreateIndex(
            name: "uix_group_member_request_guid",
            table: "group_member_request",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "group_member_request");

        migrationBuilder.DropColumn(
            name: "allow_self_registration",
            table: "group_type");

        migrationBuilder.DropColumn(
            name: "color",
            table: "group_type");

        migrationBuilder.DropColumn(
            name: "default_group_capacity",
            table: "group_type");

        migrationBuilder.DropColumn(
            name: "default_is_public",
            table: "group_type");

        migrationBuilder.DropColumn(
            name: "requires_member_approval",
            table: "group_type");
    }
}
