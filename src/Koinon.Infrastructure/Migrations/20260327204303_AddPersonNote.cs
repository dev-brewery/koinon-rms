using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person_note",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    note_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note_type_defined_value_id = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_person_note", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_note_defined_value_note_type_defined_value_id",
                        column: x => x.note_type_defined_value_id,
                        principalTable: "defined_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_note_person_alias_created_by_person_alias_id",
                        column: x => x.created_by_person_alias_id,
                        principalTable: "person_alias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_person_note_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_person_note_created_by_person_alias_id",
                table: "person_note",
                column: "created_by_person_alias_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_note_note_date",
                table: "person_note",
                column: "note_date");

            migrationBuilder.CreateIndex(
                name: "ix_person_note_note_type_defined_value_id",
                table: "person_note",
                column: "note_type_defined_value_id",
                filter: "note_type_defined_value_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_note_person_id",
                table: "person_note",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "uix_person_note_guid",
                table: "person_note",
                column: "guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_note");
        }
    }
}
