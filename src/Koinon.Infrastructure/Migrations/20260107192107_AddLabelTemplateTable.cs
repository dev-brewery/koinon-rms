using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelTemplateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "label_template",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template = table.Column<string>(type: "text", nullable: false),
                    width_mm = table.Column<int>(type: "integer", nullable: false),
                    height_mm = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_label_template", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_label_template_is_active",
                table: "label_template",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_label_template_type",
                table: "label_template",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "uix_label_template_guid",
                table: "label_template",
                column: "guid",
                unique: true);

            // Seed default label templates
            var now = DateTime.UtcNow;

            // Template 1: Child Name Label (Standard)
            migrationBuilder.InsertData(
                table: "label_template",
                columns: new[] { "guid", "name", "type", "format", "template", "width_mm", "height_mm", "is_active", "is_system", "created_date_time" },
                values: new object[] {
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Child Name Label (Standard)",
                    0, // LabelType.ChildName
                    "ZPL",
                    @"^XA
^FO50,30^A0N,50,50^FD{NickName} {LastName}^FS
^FO50,90^A0N,30,30^FD{GroupName}^FS
^FO50,130^A0N,25,25^FD{ServiceTime}^FS
^FO300,30^A0N,80,80^FD{SecurityCode}^FS
^FO300,120^A0N,20,20^FDCode: {SecurityCode}^FS
^XZ
",
                    101,
                    51,
                    true,
                    true,
                    now
                });

            // Template 2: Parent Claim Ticket (Standard)
            migrationBuilder.InsertData(
                table: "label_template",
                columns: new[] { "guid", "name", "type", "format", "template", "width_mm", "height_mm", "is_active", "is_system", "created_date_time" },
                values: new object[] {
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Parent Claim Ticket (Standard)",
                    2, // LabelType.ParentClaim
                    "ZPL",
                    @"^XA
^FO50,20^A0N,100,100^FD{SecurityCode}^FS
^FO50,130^A0N,25,25^FD{FullName}^FS
^FO50,160^A0N,20,20^FD{ServiceTime} - {CheckInTime}^FS
^XZ
",
                    76,
                    51,
                    true,
                    true,
                    now
                });

            // Template 3: Allergy Alert Label
            migrationBuilder.InsertData(
                table: "label_template",
                columns: new[] { "guid", "name", "type", "format", "template", "width_mm", "height_mm", "is_active", "is_system", "created_date_time" },
                values: new object[] {
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "Allergy Alert Label",
                    4, // LabelType.Allergy
                    "ZPL",
                    @"^XA
^FO50,20^A0N,40,40^FDALLERGY ALERT^FS
^FO50,70^A0N,30,30^FD{FullName}^FS
^FO50,110^A0N,25,25^FD{Allergies}^FS
^XZ
",
                    101,
                    51,
                    true,
                    true,
                    now
                });

            // Template 4: Child Security Label
            migrationBuilder.InsertData(
                table: "label_template",
                columns: new[] { "guid", "name", "type", "format", "template", "width_mm", "height_mm", "is_active", "is_system", "created_date_time" },
                values: new object[] {
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    "Child Security Label",
                    1, // LabelType.ChildSecurity
                    "ZPL",
                    @"^XA
^FO50,50^A0N,150,150^FD{SecurityCode}^FS
^XZ
",
                    51,
                    25,
                    true,
                    true,
                    now
                });

            // Template 5: Visitor Name Badge
            migrationBuilder.InsertData(
                table: "label_template",
                columns: new[] { "guid", "name", "type", "format", "template", "width_mm", "height_mm", "is_active", "is_system", "created_date_time" },
                values: new object[] {
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    "Visitor Name Badge",
                    3, // LabelType.VisitorName
                    "ZPL",
                    @"^XA
^FO50,30^A0N,60,60^FD{FullName}^FS
^FO50,100^A0N,30,30^FD{GroupName}^FS
^FO50,140^A0N,25,25^FD{ServiceTime}^FS
^XZ
",
                    101,
                    51,
                    true,
                    true,
                    now
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "label_template");
        }
    }
}
