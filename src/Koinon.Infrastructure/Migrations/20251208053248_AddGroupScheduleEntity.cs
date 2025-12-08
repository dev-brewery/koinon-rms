using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGroupScheduleEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "group_schedule",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                group_id = table.Column<int>(type: "integer", nullable: false),
                schedule_id = table.Column<int>(type: "integer", nullable: false),
                location_id = table.Column<int>(type: "integer", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_person_alias_id = table.Column<int>(type: "integer", nullable: true),
                modified_by_person_alias_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group_schedule", x => x.id);
                table.ForeignKey(
                    name: "FK_group_schedule_group_group_id",
                    column: x => x.group_id,
                    principalTable: "group",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_group_schedule_location_location_id",
                    column: x => x.location_id,
                    principalTable: "location",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_group_schedule_schedule_schedule_id",
                    column: x => x.schedule_id,
                    principalTable: "schedule",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_group_schedule_group_id",
            table: "group_schedule",
            column: "group_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_schedule_location_id",
            table: "group_schedule",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "ix_group_schedule_schedule_id",
            table: "group_schedule",
            column: "schedule_id");

        migrationBuilder.CreateIndex(
            name: "uix_group_schedule_group_id_schedule_id",
            table: "group_schedule",
            columns: new[] { "group_id", "schedule_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uix_group_schedule_guid",
            table: "group_schedule",
            column: "guid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "group_schedule");
    }
}
