using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonMergeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person_duplicate_ignore",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id_1 = table.Column<int>(type: "integer", nullable: false),
                    person_id_2 = table.Column<int>(type: "integer", nullable: false),
                    marked_by_person_id = table.Column<int>(type: "integer", nullable: true),
                    marked_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_duplicate_ignore", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_duplicate_ignore_person_marked_by_person_id",
                        column: x => x.marked_by_person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_duplicate_ignore_person_person_id_1",
                        column: x => x.person_id_1,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_duplicate_ignore_person_person_id_2",
                        column: x => x.person_id_2,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_merge_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    survivor_person_id = table.Column<int>(type: "integer", nullable: false),
                    merged_person_id = table.Column<int>(type: "integer", nullable: false),
                    merged_by_person_id = table.Column<int>(type: "integer", nullable: true),
                    merged_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_merge_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_merge_history_person_merged_by_person_id",
                        column: x => x.merged_by_person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_merge_history_person_merged_person_id",
                        column: x => x.merged_person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_merge_history_person_survivor_person_id",
                        column: x => x.survivor_person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_person_duplicate_ignore_marked_by_person_id",
                table: "person_duplicate_ignore",
                column: "marked_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_duplicate_ignore_person_id_1",
                table: "person_duplicate_ignore",
                column: "person_id_1");

            migrationBuilder.CreateIndex(
                name: "ix_person_duplicate_ignore_person_id_2",
                table: "person_duplicate_ignore",
                column: "person_id_2");

            migrationBuilder.CreateIndex(
                name: "uix_person_duplicate_ignore_guid",
                table: "person_duplicate_ignore",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_person_duplicate_ignore_person_ids",
                table: "person_duplicate_ignore",
                columns: new[] { "person_id_1", "person_id_2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_merge_history_merged_by_person_id",
                table: "person_merge_history",
                column: "merged_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_merge_history_merged_date_time",
                table: "person_merge_history",
                column: "merged_date_time");

            migrationBuilder.CreateIndex(
                name: "ix_person_merge_history_merged_person_id",
                table: "person_merge_history",
                column: "merged_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_merge_history_survivor_person_id",
                table: "person_merge_history",
                column: "survivor_person_id");

            migrationBuilder.CreateIndex(
                name: "uix_person_merge_history_guid",
                table: "person_merge_history",
                column: "guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_duplicate_ignore");

            migrationBuilder.DropTable(
                name: "person_merge_history");
        }
    }
}
