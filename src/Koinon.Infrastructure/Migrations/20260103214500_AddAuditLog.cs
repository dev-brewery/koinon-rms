using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                    person_id = table.Column<int>(type: "integer", nullable: true),
                    action_type = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    changed_properties = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    additional_info = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_log_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "uix_audit_log_guid",
                table: "audit_log",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_timestamp",
                table: "audit_log",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_type_entity_id_key",
                table: "audit_log",
                columns: new[] { "entity_type", "entity_id_key" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_person_id",
                table: "audit_log",
                column: "person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");
        }
    }
}
