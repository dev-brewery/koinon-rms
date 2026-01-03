using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCommunicationPreference : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "communication_preference",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                communication_type = table.Column<int>(type: "integer", nullable: false),
                is_opted_out = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                opt_out_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                opt_out_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_communication_preference", x => x.id);
                table.ForeignKey(
                    name: "FK_communication_preference_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_communication_preference_person_id",
            table: "communication_preference",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "uix_communication_preference_guid",
            table: "communication_preference",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_communication_preference_person_type",
            table: "communication_preference",
            columns: new[] { "person_id", "communication_type" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "communication_preference");
    }
}
