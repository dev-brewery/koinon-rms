using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:postgis", ",,");

        migrationBuilder.CreateTable(
            name: "defined_type",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                help_text = table.Column<string>(type: "text", nullable: true),
                is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                field_type_assembly_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_defined_type", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "group_type",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                group_term = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Group"),
                group_member_term = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Member"),
                default_group_role_id = table.Column<int>(type: "integer", nullable: true),
                icon_css_class = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                allow_multiple_locations = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                show_in_group_list = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                show_in_navigation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                takes_attendance = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                attendance_counts_as_weekend_service = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                send_attendance_reminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                show_connection_status = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                enable_specific_group_requirements = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                allow_group_sync = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                allow_specific_group_member_attributes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                group_type_purpose_value_id = table.Column<int>(type: "integer", nullable: true),
                ignore_person_inactivated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                archived_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                archived_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_family_group_type = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group_type", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "defined_value",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                defined_type_id = table.Column<int>(type: "integer", nullable: false),
                value = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_defined_value", x => x.id);
                table.ForeignKey(
                    name: "FK_defined_value_defined_type_defined_type_id",
                    column: x => x.defined_type_id,
                    principalTable: "defined_type",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "group_type_role",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                group_type_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                is_leader = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                can_view = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                can_edit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                can_manage_members = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                receive_requirements_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                max_count = table.Column<int>(type: "integer", nullable: true),
                min_count = table.Column<int>(type: "integer", nullable: true),
                is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group_type_role", x => x.id);
                table.ForeignKey(
                    name: "FK_group_type_role_group_type_group_type_id",
                    column: x => x.group_type_id,
                    principalTable: "group_type",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "campus",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                short_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                time_zone_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                campus_status_value_id = table.Column<int>(type: "integer", nullable: true),
                leader_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                service_times = table.Column<string>(type: "text", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_campus", x => x.id);
                table.ForeignKey(
                    name: "FK_campus_defined_value_campus_status_value_id",
                    column: x => x.campus_status_value_id,
                    principalTable: "defined_value",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "location",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                parent_location_id = table.Column<int>(type: "integer", nullable: true),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                location_type_value_id = table.Column<int>(type: "integer", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                printer_device_id = table.Column<int>(type: "integer", nullable: true),
                image_id = table.Column<int>(type: "integer", nullable: true),
                soft_room_threshold = table.Column<int>(type: "integer", nullable: true),
                firm_room_threshold = table.Column<int>(type: "integer", nullable: true),
                is_geo_point_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                street1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                street2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                latitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: true),
                longitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_location", x => x.id);
                table.ForeignKey(
                    name: "FK_location_defined_value_location_type_value_id",
                    column: x => x.location_type_value_id,
                    principalTable: "defined_value",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_location_location_parent_location_id",
                    column: x => x.parent_location_id,
                    principalTable: "location",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "group",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                group_type_id = table.Column<int>(type: "integer", nullable: false),
                parent_group_id = table.Column<int>(type: "integer", nullable: true),
                campus_id = table.Column<int>(type: "integer", nullable: true),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                is_security_role = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                archived_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                archived_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                allow_guests = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                group_capacity = table.Column<int>(type: "integer", nullable: true),
                schedule_id = table.Column<int>(type: "integer", nullable: true),
                welcome_system_communication_id = table.Column<int>(type: "integer", nullable: true),
                exit_system_communication_id = table.Column<int>(type: "integer", nullable: true),
                required_signature_document_template_id = table.Column<int>(type: "integer", nullable: true),
                status_value_id = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group", x => x.id);
                table.ForeignKey(
                    name: "FK_group_campus_campus_id",
                    column: x => x.campus_id,
                    principalTable: "campus",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_group_group_parent_group_id",
                    column: x => x.parent_group_id,
                    principalTable: "group",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_group_group_type_group_type_id",
                    column: x => x.group_type_id,
                    principalTable: "group_type",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "person",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                is_system = table.Column<bool>(type: "boolean", nullable: false),
                record_type_value_id = table.Column<int>(type: "integer", nullable: true),
                record_status_value_id = table.Column<int>(type: "integer", nullable: true),
                record_status_reason_value_id = table.Column<int>(type: "integer", nullable: true),
                connection_status_value_id = table.Column<int>(type: "integer", nullable: true),
                review_reason_note = table.Column<string>(type: "text", nullable: true),
                is_deceased = table.Column<bool>(type: "boolean", nullable: false),
                title_value_id = table.Column<int>(type: "integer", nullable: true),
                first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                nick_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                middle_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                suffix_value_id = table.Column<int>(type: "integer", nullable: true),
                photo_id = table.Column<int>(type: "integer", nullable: true),
                birth_day = table.Column<int>(type: "integer", nullable: true),
                birth_month = table.Column<int>(type: "integer", nullable: true),
                birth_year = table.Column<int>(type: "integer", nullable: true),
                gender = table.Column<int>(type: "integer", nullable: false),
                marital_status_value_id = table.Column<int>(type: "integer", nullable: true),
                anniversary_date = table.Column<DateOnly>(type: "date", nullable: true),
                graduation_year = table.Column<int>(type: "integer", nullable: true),
                giving_group_id = table.Column<int>(type: "integer", nullable: true),
                email = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                is_email_active = table.Column<bool>(type: "boolean", nullable: false),
                email_note = table.Column<string>(type: "text", nullable: true),
                email_preference = table.Column<int>(type: "integer", nullable: false),
                communication_preference = table.Column<int>(type: "integer", nullable: true),
                inactive_reason_note = table.Column<string>(type: "text", nullable: true),
                system_note = table.Column<string>(type: "text", nullable: true),
                primary_family_id = table.Column<int>(type: "integer", nullable: true),
                primary_campus_id = table.Column<int>(type: "integer", nullable: true),
                search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "\n                    setweight(to_tsvector('english', coalesce(first_name, '')), 'A') ||\n                    setweight(to_tsvector('english', coalesce(last_name, '')), 'A') ||\n                    setweight(to_tsvector('english', coalesce(nick_name, '')), 'B') ||\n                    setweight(to_tsvector('english', coalesce(email, '')), 'C')\n                    ", stored: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_person", x => x.id);
                table.ForeignKey(
                    name: "FK_person_group_primary_family_id",
                    column: x => x.primary_family_id,
                    principalTable: "group",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "group_member",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                person_id = table.Column<int>(type: "integer", nullable: false),
                group_id = table.Column<int>(type: "integer", nullable: false),
                group_role_id = table.Column<int>(type: "integer", nullable: false),
                group_member_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                date_time_added = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                inactive_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                archived_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                archived_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_notified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                communication_preference = table.Column<int>(type: "integer", nullable: true),
                guest_count = table.Column<int>(type: "integer", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group_member", x => x.id);
                table.ForeignKey(
                    name: "FK_group_member_group_group_id",
                    column: x => x.group_id,
                    principalTable: "group",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_group_member_group_type_role_group_role_id",
                    column: x => x.group_role_id,
                    principalTable: "group_type_role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_group_member_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "person_alias",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: true),
                name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                alias_person_id = table.Column<int>(type: "integer", nullable: true),
                alias_person_guid = table.Column<Guid>(type: "uuid", nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_person_alias", x => x.id);
                table.ForeignKey(
                    name: "FK_person_alias_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "phone_number",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                extension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                number_type_value_id = table.Column<int>(type: "integer", nullable: true),
                is_messaging_enabled = table.Column<bool>(type: "boolean", nullable: false),
                is_unlisted = table.Column<bool>(type: "boolean", nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_phone_number", x => x.id);
                table.ForeignKey(
                    name: "FK_phone_number_defined_value_number_type_value_id",
                    column: x => x.number_type_value_id,
                    principalTable: "defined_value",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_phone_number_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_campus_campus_status_value_id",
            table: "campus",
            column: "campus_status_value_id");

        migrationBuilder.CreateIndex(
            name: "ix_campus_is_active",
            table: "campus",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_campus_order",
            table: "campus",
            column: "order");

        migrationBuilder.CreateIndex(
            name: "ix_campus_short_code",
            table: "campus",
            column: "short_code",
            filter: "short_code IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "uix_campus_guid",
            table: "campus",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_defined_type_category",
            table: "defined_type",
            column: "category");

        migrationBuilder.CreateIndex(
            name: "ix_defined_type_is_system",
            table: "defined_type",
            column: "is_system");

        migrationBuilder.CreateIndex(
            name: "ix_defined_type_name",
            table: "defined_type",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "uix_defined_type_guid",
            table: "defined_type",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_defined_value_defined_type_id",
            table: "defined_value",
            column: "defined_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_defined_value_is_active",
            table: "defined_value",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_defined_value_order",
            table: "defined_value",
            column: "order");

        migrationBuilder.CreateIndex(
            name: "ix_defined_value_type_active_order",
            table: "defined_value",
            columns: new[] { "defined_type_id", "is_active", "order" });

        migrationBuilder.CreateIndex(
            name: "ix_defined_value_value",
            table: "defined_value",
            column: "value");

        migrationBuilder.CreateIndex(
            name: "uix_defined_value_guid",
            table: "defined_value",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_group_campus_id",
            table: "group",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_group_type_id",
            table: "group",
            column: "group_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_is_active",
            table: "group",
            column: "is_active",
            filter: "is_active = true");

        migrationBuilder.CreateIndex(
            name: "ix_group_is_archived",
            table: "group",
            column: "is_archived",
            filter: "is_archived = false");

        migrationBuilder.CreateIndex(
            name: "ix_group_name",
            table: "group",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "ix_group_parent_group_id",
            table: "group",
            column: "parent_group_id");

        migrationBuilder.CreateIndex(
            name: "uix_group_guid",
            table: "group",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_group_member_group_id",
            table: "group_member",
            column: "group_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_member_group_role_id",
            table: "group_member",
            column: "group_role_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_member_person_id",
            table: "group_member",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_member_status",
            table: "group_member",
            column: "group_member_status");

        migrationBuilder.CreateIndex(
            name: "uix_group_member_group_person_role",
            table: "group_member",
            columns: new[] { "group_id", "person_id", "group_role_id" },
            unique: true,
            filter: "is_archived = false");

        migrationBuilder.CreateIndex(
            name: "uix_group_member_guid",
            table: "group_member",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_group_type_is_family_group_type",
            table: "group_type",
            column: "is_family_group_type",
            filter: "is_family_group_type = true");

        migrationBuilder.CreateIndex(
            name: "ix_group_type_name",
            table: "group_type",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "uix_group_type_guid",
            table: "group_type",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_group_type_role_group_type_id",
            table: "group_type_role",
            column: "group_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_type_role_group_type_id_name",
            table: "group_type_role",
            columns: new[] { "group_type_id", "name" });

        migrationBuilder.CreateIndex(
            name: "uix_group_type_role_guid",
            table: "group_type_role",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_location_is_active",
            table: "location",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_location_location_type_value_id",
            table: "location",
            column: "location_type_value_id");

        migrationBuilder.CreateIndex(
            name: "ix_location_name",
            table: "location",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "ix_location_parent_location_id",
            table: "location",
            column: "parent_location_id");

        migrationBuilder.CreateIndex(
            name: "uix_location_guid",
            table: "location",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_person_email",
            table: "person",
            column: "email",
            filter: "email IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_person_last_name_first_name",
            table: "person",
            columns: new[] { "last_name", "first_name" });

        migrationBuilder.CreateIndex(
            name: "ix_person_primary_campus_id",
            table: "person",
            column: "primary_campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_person_primary_family_id",
            table: "person",
            column: "primary_family_id");

        migrationBuilder.CreateIndex(
            name: "ix_person_record_status_value_id",
            table: "person",
            column: "record_status_value_id");

        migrationBuilder.CreateIndex(
            name: "ix_person_search_vector",
            table: "person",
            column: "search_vector")
            .Annotation("Npgsql:IndexMethod", "GIN");

        migrationBuilder.CreateIndex(
            name: "uix_person_guid",
            table: "person",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_person_alias_person_id",
            table: "person_alias",
            column: "person_id",
            filter: "person_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "uix_person_alias_guid",
            table: "person_alias",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_phone_number_number_type_value_id",
            table: "phone_number",
            column: "number_type_value_id");

        migrationBuilder.CreateIndex(
            name: "ix_phone_number_person_id",
            table: "phone_number",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_phone_number_person_number",
            table: "phone_number",
            columns: new[] { "person_id", "number" });

        migrationBuilder.CreateIndex(
            name: "uix_phone_number_guid",
            table: "phone_number",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "group_member");

        migrationBuilder.DropTable(
            name: "location");

        migrationBuilder.DropTable(
            name: "person_alias");

        migrationBuilder.DropTable(
            name: "phone_number");

        migrationBuilder.DropTable(
            name: "group_type_role");

        migrationBuilder.DropTable(
            name: "person");

        migrationBuilder.DropTable(
            name: "group");

        migrationBuilder.DropTable(
            name: "campus");

        migrationBuilder.DropTable(
            name: "group_type");

        migrationBuilder.DropTable(
            name: "defined_value");

        migrationBuilder.DropTable(
            name: "defined_type");
    }
}
