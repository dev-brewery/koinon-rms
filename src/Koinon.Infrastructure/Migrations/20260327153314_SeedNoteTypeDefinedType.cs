using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeedNoteTypeDefinedType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        // Insert Note Type DefinedType
        migrationBuilder.InsertData(
            table: "defined_type",
            columns: new[] { "guid", "name", "description", "category", "is_system", "order", "created_date_time" },
            values: new object[,]
            {
                { Guid.Parse("3F8A2C1D-7E5B-4F9A-B3D6-1C4E8F2A5B7D"), "Note Type", "Type of pastoral note or interaction log entry", "Person", true, 10, now }
            });

        // Note Type DefinedType GUID
        var noteTypeGuid = "3F8A2C1D-7E5B-4F9A-B3D6-1C4E8F2A5B7D";

        // Insert DefinedValues for Note Type
        // SQL OK: All interpolated values are compile-time GUIDs and controlled DateTime
        migrationBuilder.Sql($@"
                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")}',
                    id,
                    'General',
                    'General pastoral note',
                    0,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{noteTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("B2C3D4E5-F6A7-8901-BCDE-F12345678901")}',
                    id,
                    'Prayer Request',
                    'A prayer request submitted by or for the person',
                    1,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{noteTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("C3D4E5F6-A7B8-9012-CDEF-012345678902")}',
                    id,
                    'Pastoral Visit',
                    'Record of a pastoral visit or home visit',
                    2,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{noteTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("D4E5F6A7-B8C9-0123-DEF0-123456789003")}',
                    id,
                    'Counseling',
                    'Record of a counseling session',
                    3,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{noteTypeGuid}';
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete DefinedValues first (due to foreign key constraints)
        migrationBuilder.Sql(@"
                DELETE FROM defined_value WHERE guid IN (
                    'A1B2C3D4-E5F6-7890-ABCD-EF1234567890',
                    'B2C3D4E5-F6A7-8901-BCDE-F12345678901',
                    'C3D4E5F6-A7B8-9012-CDEF-012345678902',
                    'D4E5F6A7-B8C9-0123-DEF0-123456789003'
                );
            ");

        // Delete DefinedType
        migrationBuilder.Sql(@"
                DELETE FROM defined_type WHERE guid = '3F8A2C1D-7E5B-4F9A-B3D6-1C4E8F2A5B7D';
            ");
    }
}
