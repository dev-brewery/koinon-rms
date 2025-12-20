using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_person_group_primary_family_id",
                table: "person");

            migrationBuilder.DropIndex(
                name: "ix_person_primary_family_id",
                table: "person");

            migrationBuilder.DropIndex(
                name: "ix_group_type_is_family_group_type",
                table: "group_type");

            migrationBuilder.DropColumn(
                name: "primary_family_id",
                table: "person");

            migrationBuilder.DropColumn(
                name: "is_family_group_type",
                table: "group_type");

            migrationBuilder.CreateTable(
                name: "family",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    campus_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByPersonAliasId = table.Column<int>(type: "integer", nullable: true),
                    ModifiedByPersonAliasId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family", x => x.id);
                    table.ForeignKey(
                        name: "FK_family_campus_campus_id",
                        column: x => x.campus_id,
                        principalTable: "campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_template",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    import_type = table.Column<int>(type: "integer", nullable: false),
                    field_mappings = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "family_member",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    family_id = table.Column<int>(type: "integer", nullable: false),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    family_role_id = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    date_added = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByPersonAliasId = table.Column<int>(type: "integer", nullable: true),
                    ModifiedByPersonAliasId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_member", x => x.id);
                    table.ForeignKey(
                        name: "FK_family_member_family_family_id",
                        column: x => x.family_id,
                        principalTable: "family",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_family_member_group_type_role_family_role_id",
                        column: x => x.family_role_id,
                        principalTable: "group_type_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_member_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "import_job",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_template_id = table.Column<int>(type: "integer", nullable: true),
                    import_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    success_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_details = table.Column<string>(type: "jsonb", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_job", x => x.id);
                    table.ForeignKey(
                        name: "FK_import_job_import_template_import_template_id",
                        column: x => x.import_template_id,
                        principalTable: "import_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_family_campus_id",
                table: "family",
                column: "campus_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_name",
                table: "family",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "uix_family_guid",
                table: "family",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_family_member_family_id",
                table: "family_member",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_member_family_role_id",
                table: "family_member",
                column: "family_role_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_member_person_id",
                table: "family_member",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_member_person_primary",
                table: "family_member",
                columns: new[] { "person_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "uix_family_member_family_person",
                table: "family_member",
                columns: new[] { "family_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_family_member_guid",
                table: "family_member",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_import_job_created_date_time",
                table: "import_job",
                column: "created_date_time");

            migrationBuilder.CreateIndex(
                name: "IX_import_job_import_template_id",
                table: "import_job",
                column: "import_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_import_job_import_type",
                table: "import_job",
                column: "import_type");

            migrationBuilder.CreateIndex(
                name: "ix_import_job_status",
                table: "import_job",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_import_job_status_created",
                table: "import_job",
                columns: new[] { "status", "created_date_time" });

            migrationBuilder.CreateIndex(
                name: "uix_import_job_guid",
                table: "import_job",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_import_template_import_type",
                table: "import_template",
                column: "import_type");

            migrationBuilder.CreateIndex(
                name: "ix_import_template_is_active",
                table: "import_template",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "uix_import_template_guid",
                table: "import_template",
                column: "guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_member");

            migrationBuilder.DropTable(
                name: "import_job");

            migrationBuilder.DropTable(
                name: "family");

            migrationBuilder.DropTable(
                name: "import_template");

            migrationBuilder.AddColumn<int>(
                name: "primary_family_id",
                table: "person",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_family_group_type",
                table: "group_type",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_person_primary_family_id",
                table: "person",
                column: "primary_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_type_is_family_group_type",
                table: "group_type",
                column: "is_family_group_type",
                filter: "is_family_group_type = true");

            migrationBuilder.AddForeignKey(
                name: "FK_person_group_primary_family_id",
                table: "person",
                column: "primary_family_id",
                principalTable: "group",
                principalColumn: "id");
        }
    }
}
