using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddUserSettingsEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "storage_key",
            table: "import_job",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "background_job_id",
            table: "import_job",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "two_factor_config",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                secret_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                recovery_codes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_two_factor_config", x => x.id);
                table.ForeignKey(
                    name: "FK_two_factor_config_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_preference",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                theme = table.Column<int>(type: "integer", nullable: false),
                date_format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_preference", x => x.id);
                table.ForeignKey(
                    name: "FK_user_preference_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_session",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                refresh_token_id = table.Column<int>(type: "integer", nullable: true),
                device_info = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                location = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_session", x => x.id);
                table.ForeignKey(
                    name: "FK_user_session_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_user_session_refresh_token_refresh_token_id",
                    column: x => x.refresh_token_id,
                    principalTable: "refresh_token",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "uix_two_factor_config_guid",
            table: "two_factor_config",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_two_factor_config_person_id",
            table: "two_factor_config",
            column: "person_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_preference_person_id",
            table: "user_preference",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "uix_user_preference_guid",
            table: "user_preference",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_session_person_id",
            table: "user_session",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_session_refresh_token_id",
            table: "user_session",
            column: "refresh_token_id");

        migrationBuilder.CreateIndex(
            name: "uix_user_session_guid",
            table: "user_session",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "two_factor_config");

        migrationBuilder.DropTable(
            name: "user_preference");

        migrationBuilder.DropTable(
            name: "user_session");

        migrationBuilder.AlterColumn<string>(
            name: "storage_key",
            table: "import_job",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(500)",
            oldMaxLength: 500,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "background_job_id",
            table: "import_job",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);
    }
}
