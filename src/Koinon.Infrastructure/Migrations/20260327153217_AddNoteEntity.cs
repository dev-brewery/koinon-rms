using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddNoteEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "note",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_alias_id = table.Column<int>(type: "integer", nullable: false),
                note_type_value_id = table.Column<int>(type: "integer", nullable: false),
                text = table.Column<string>(type: "text", nullable: false),
                note_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                author_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                is_private = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_alert = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_note", x => x.id);
                table.ForeignKey(
                    name: "FK_note_defined_value_note_type_value_id",
                    column: x => x.note_type_value_id,
                    principalTable: "defined_value",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_note_person_alias_author_person_alias_id",
                    column: x => x.author_person_alias_id,
                    principalTable: "person_alias",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_note_person_alias_person_alias_id",
                    column: x => x.person_alias_id,
                    principalTable: "person_alias",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_note_author_person_alias_id",
            table: "note",
            column: "author_person_alias_id");

        migrationBuilder.CreateIndex(
            name: "ix_note_note_type_value_id",
            table: "note",
            column: "note_type_value_id");

        migrationBuilder.CreateIndex(
            name: "ix_note_person_alias_id_note_date_time",
            table: "note",
            columns: new[] { "person_alias_id", "note_date_time" });

        migrationBuilder.CreateIndex(
            name: "uix_note_guid",
            table: "note",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "note");
    }
}
