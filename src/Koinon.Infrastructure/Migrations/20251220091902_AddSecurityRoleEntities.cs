using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityRoleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "security_claim",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    claim_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    claim_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_claim", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "security_role",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system_role = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person_security_role",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    security_role_id = table.Column<int>(type: "integer", nullable: false),
                    expires_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_security_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_security_role_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_person_security_role_security_role_security_role_id",
                        column: x => x.security_role_id,
                        principalTable: "security_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_security_claim",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    security_role_id = table.Column<int>(type: "integer", nullable: false),
                    security_claim_id = table.Column<int>(type: "integer", nullable: false),
                    allow_or_deny = table.Column<char>(type: "char(1)", nullable: false, comment: "A=Allow, D=Deny"),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_security_claim", x => x.id);
                    table.CheckConstraint("ck_role_claim_allow_deny", "allow_or_deny IN ('A', 'D')");
                    table.ForeignKey(
                        name: "FK_role_security_claim_security_claim_security_claim_id",
                        column: x => x.security_claim_id,
                        principalTable: "security_claim",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_security_claim_security_role_security_role_id",
                        column: x => x.security_role_id,
                        principalTable: "security_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_person_security_role_expires_date_time",
                table: "person_security_role",
                column: "expires_date_time");

            migrationBuilder.CreateIndex(
                name: "ix_person_security_role_person_id",
                table: "person_security_role",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_security_role_security_role_id",
                table: "person_security_role",
                column: "security_role_id");

            migrationBuilder.CreateIndex(
                name: "uix_person_security_role_guid",
                table: "person_security_role",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_security_claim_security_claim_id",
                table: "role_security_claim",
                column: "security_claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_security_claim_security_role_id",
                table: "role_security_claim",
                column: "security_role_id");

            migrationBuilder.CreateIndex(
                name: "uix_role_security_claim_guid",
                table: "role_security_claim",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_role_security_claim_role_claim",
                table: "role_security_claim",
                columns: new[] { "security_role_id", "security_claim_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_security_claim_guid",
                table: "security_claim",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_security_claim_type_value",
                table: "security_claim",
                columns: new[] { "claim_type", "claim_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_role_is_active",
                table: "security_role",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "uix_security_role_guid",
                table: "security_role",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_security_role_name",
                table: "security_role",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_security_role");

            migrationBuilder.DropTable(
                name: "role_security_claim");

            migrationBuilder.DropTable(
                name: "security_claim");

            migrationBuilder.DropTable(
                name: "security_role");
        }
    }
}
