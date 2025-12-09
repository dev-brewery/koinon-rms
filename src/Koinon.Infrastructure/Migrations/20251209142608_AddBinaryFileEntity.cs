using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddBinaryFileEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "binary_file",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                width = table.Column<int>(type: "integer", nullable: true),
                height = table.Column<int>(type: "integer", nullable: true),
                binary_file_type_id = table.Column<int>(type: "integer", nullable: true),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_binary_file", x => x.id);
                table.ForeignKey(
                    name: "FK_binary_file_defined_value_binary_file_type_id",
                    column: x => x.binary_file_type_id,
                    principalTable: "defined_value",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_person_photo_id",
            table: "person",
            column: "photo_id");

        migrationBuilder.CreateIndex(
            name: "IX_binary_file_binary_file_type_id",
            table: "binary_file",
            column: "binary_file_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_binary_file_created_date_time",
            table: "binary_file",
            column: "created_date_time");

        migrationBuilder.CreateIndex(
            name: "IX_binary_file_guid",
            table: "binary_file",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_binary_file_mime_type",
            table: "binary_file",
            column: "mime_type");

        migrationBuilder.CreateIndex(
            name: "IX_binary_file_storage_key",
            table: "binary_file",
            column: "storage_key",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_person_binary_file_photo_id",
            table: "person",
            column: "photo_id",
            principalTable: "binary_file",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_person_binary_file_photo_id",
            table: "person");

        migrationBuilder.DropTable(
            name: "binary_file");

        migrationBuilder.DropIndex(
            name: "IX_person_photo_id",
            table: "person");
    }
}
