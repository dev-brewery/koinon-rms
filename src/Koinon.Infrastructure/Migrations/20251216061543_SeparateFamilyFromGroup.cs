using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeparateFamilyFromGroup : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Step 1: Create family table
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
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

        // Step 2: Create family_member table
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
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_family_member", x => x.id);
                table.ForeignKey(
                    name: "FK_family_member_group_type_role_family_role_id",
                    column: x => x.family_role_id,
                    principalTable: "group_type_role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_family_member_family_family_id",
                    column: x => x.family_id,
                    principalTable: "family",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_family_member_person_person_id",
                    column: x => x.person_id,
                    principalTable: "person",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Step 3: Create indexes on family table
        migrationBuilder.CreateIndex(
            name: "uix_family_guid",
            table: "family",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_family_campus_id",
            table: "family",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_family_name",
            table: "family",
            column: "name");

        // Step 4: Create indexes on family_member table
        migrationBuilder.CreateIndex(
            name: "uix_family_member_guid",
            table: "family_member",
            column: "guid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_family_member_family_id",
            table: "family_member",
            column: "family_id");

        migrationBuilder.CreateIndex(
            name: "ix_family_member_person_id",
            table: "family_member",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ix_family_member_family_role_id",
            table: "family_member",
            column: "family_role_id");

        migrationBuilder.CreateIndex(
            name: "uix_family_member_family_person",
            table: "family_member",
            columns: new[] { "family_id", "person_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_family_member_person_primary",
            table: "family_member",
            columns: new[] { "person_id", "is_primary" });

        // Step 5: Migrate existing family data from group to family
        migrationBuilder.Sql(@"
            -- Insert families from groups marked as family type
            INSERT INTO family (id, name, campus_id, is_active, guid, created_date_time, modified_date_time)
            SELECT 
                g.id,
                g.name,
                g.campus_id,
                g.is_active,
                g.guid,
                g.created_date_time,
                g.modified_date_time
            FROM ""group"" g
            INNER JOIN group_type gt ON g.group_type_id = gt.id
            WHERE gt.is_family_group_type = true;

            -- Migrate family members from group_member
            INSERT INTO family_member (family_id, person_id, family_role_id, is_primary, date_added, guid, created_date_time, modified_date_time)
            SELECT 
                gm.group_id,
                gm.person_id,
                gm.group_role_id,
                CASE WHEN p.primary_family_id = gm.group_id THEN true ELSE false END,
                gm.date_time_added,
                gen_random_uuid(),
                gm.created_date_time,
                gm.modified_date_time
            FROM group_member gm
            INNER JOIN ""group"" g ON gm.group_id = g.id
            INNER JOIN group_type gt ON g.group_type_id = gt.id
            LEFT JOIN person p ON gm.person_id = p.id
            WHERE gt.is_family_group_type = true;
        ");

        // Step 6: Drop primary_family_id column from person table (after data migrated)
        migrationBuilder.DropIndex(
            name: "ix_person_primary_family_id",
            table: "person");

        migrationBuilder.DropColumn(
            name: "primary_family_id",
            table: "person");

        // Step 7: Delete family groups and their members from group/group_member tables
        migrationBuilder.Sql(@"
            -- Delete group members for family groups
            DELETE FROM group_member
            WHERE group_id IN (
                SELECT g.id 
                FROM ""group"" g
                INNER JOIN group_type gt ON g.group_type_id = gt.id
                WHERE gt.is_family_group_type = true
            );

            -- Delete family groups
            DELETE FROM ""group""
            WHERE group_type_id IN (
                SELECT id FROM group_type WHERE is_family_group_type = true
            );

            -- Delete family group types
            DELETE FROM group_type WHERE is_family_group_type = true;
        ");

        // Step 8: Drop is_family_group_type column from group_type table
        migrationBuilder.DropIndex(
            name: "ix_group_type_is_family_group_type",
            table: "group_type");

        migrationBuilder.DropColumn(
            name: "is_family_group_type",
            table: "group_type");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Step 1: Re-add is_family_group_type column to group_type
        migrationBuilder.AddColumn<bool>(
            name: "is_family_group_type",
            table: "group_type",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "ix_group_type_is_family_group_type",
            table: "group_type",
            column: "is_family_group_type",
            filter: "is_family_group_type = true");

        // Step 2: Re-add primary_family_id column to person
        migrationBuilder.AddColumn<int>(
            name: "primary_family_id",
            table: "person",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_person_primary_family_id",
            table: "person",
            column: "primary_family_id");

        // Step 3: Migrate family data back to group
        migrationBuilder.Sql(@"
            -- Re-create family group type (use a fixed ID if one existed)
            INSERT INTO group_type (name, is_family_group_type, guid, created_date_time)
            VALUES ('Family', true, gen_random_uuid(), NOW())
            ON CONFLICT DO NOTHING;

            -- Migrate families back to groups
            INSERT INTO ""group"" (id, name, group_type_id, campus_id, is_active, guid, created_date_time, modified_date_time)
            SELECT 
                f.id,
                f.name,
                (SELECT id FROM group_type WHERE is_family_group_type = true LIMIT 1),
                f.campus_id,
                f.is_active,
                f.guid,
                f.created_date_time,
                f.modified_date_time
            FROM family f;

            -- Restore primary_family_id in person table BEFORE dropping family_member
            UPDATE person p
            SET primary_family_id = fm.family_id
            FROM family_member fm
            WHERE p.id = fm.person_id AND fm.is_primary = true;

            -- Migrate family members back to group_member
            INSERT INTO group_member (group_id, person_id, group_role_id, date_time_added, group_member_status, guid, created_date_time, modified_date_time)
            SELECT 
                fm.family_id,
                fm.person_id,
                fm.family_role_id,
                fm.date_added,
                0, -- Active status
                fm.guid,
                fm.created_date_time,
                fm.modified_date_time
            FROM family_member fm;
        ");

        // Step 4: Drop family tables
        migrationBuilder.DropTable(name: "family_member");
        migrationBuilder.DropTable(name: "family");
    }
}
