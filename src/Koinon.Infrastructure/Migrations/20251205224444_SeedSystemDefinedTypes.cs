using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeedSystemDefinedTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        // Insert DefinedTypes
        migrationBuilder.InsertData(
            table: "defined_type",
            columns: new[] { "guid", "name", "description", "category", "is_system", "order", "created_date_time" },
            values: new object[,]
            {
                { Guid.Parse("8522BADD-EBE0-41A9-9F1D-A132A9B9C80A"), "Person Record Status", "Status of a person record (Active, Inactive, Pending)", "Person", true, 0, now },
                { Guid.Parse("2E6540EA-63F0-40FE-BE50-F2A84735E600"), "Person Connection Status", "Connection status of a person (Member, Attendee, Visitor, Prospect)", "Person", true, 1, now },
                { Guid.Parse("8345DD45-73C6-4F5E-BEBD-B77FC83F18FD"), "Phone Number Type", "Type of phone number (Mobile, Home, Work)", "Person", true, 2, now },
                { Guid.Parse("26A0B2DE-72A1-4558-9B49-4BDE9572F6BC"), "Marital Status", "Marital status of a person", "Person", true, 3, now }
            });

        // Get the IDs of the DefinedTypes we just created (using GUIDs)
        var recordStatusTypeGuid = "8522BADD-EBE0-41A9-9F1D-A132A9B9C80A";
        var connectionStatusTypeGuid = "2E6540EA-63F0-40FE-BE50-F2A84735E600";
        var phoneTypeGuid = "8345DD45-73C6-4F5E-BEBD-B77FC83F18FD";
        var maritalStatusTypeGuid = "26A0B2DE-72A1-4558-9B49-4BDE9572F6BC";

        // Insert DefinedValues for Person Record Status
        migrationBuilder.Sql($@"
                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("618F906C-C33D-4FA3-8AEF-E58CB7B63F1E")}',
                    id,
                    'Active',
                    'Person record is active',
                    0,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{recordStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("1DAD99D5-41A9-4865-8366-F269902B80A4")}',
                    id,
                    'Inactive',
                    'Person record is inactive',
                    1,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{recordStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("283999EC-7346-42E3-B807-BCE9B2BABB49")}',
                    id,
                    'Pending',
                    'Person record is pending',
                    2,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{recordStatusTypeGuid}';
            ");

        // Insert DefinedValues for Person Connection Status
        migrationBuilder.Sql($@"
                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("41540783-D9EF-4C70-8F1D-C9E83D91ED5F")}',
                    id,
                    'Member',
                    'Person is a member',
                    0,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{connectionStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("39F491C5-D6AC-4A9B-8AC0-C431CB17D588")}',
                    id,
                    'Attendee',
                    'Person is an attendee',
                    1,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{connectionStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("B91BA046-BC1E-400C-B85D-638C1F4E0CE2")}',
                    id,
                    'Visitor',
                    'Person is a visitor',
                    2,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{connectionStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("368DD475-242C-49C4-A42C-7278BE690CC2")}',
                    id,
                    'Prospect',
                    'Person is a prospect',
                    3,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{connectionStatusTypeGuid}';
            ");

        // Insert DefinedValues for Phone Number Type
        migrationBuilder.Sql($@"
                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("407E7E45-7B2E-4FCD-9605-ECB1339F2453")}',
                    id,
                    'Mobile',
                    'Mobile phone number',
                    0,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{phoneTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("AA8732FB-2CEA-4C76-8D6D-6AAA2C6A4303")}',
                    id,
                    'Home',
                    'Home phone number',
                    1,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{phoneTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("2CC66D5A-F61C-4B74-9AF9-590A9847C13C")}',
                    id,
                    'Work',
                    'Work phone number',
                    2,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{phoneTypeGuid}';
            ");

        // Insert DefinedValues for Marital Status
        migrationBuilder.Sql($@"
                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("71F123EF-3F48-4E42-973E-96F265D3D9F6")}',
                    id,
                    'Single',
                    'Single/Never married',
                    0,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{maritalStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("F9799606-F5FD-479A-848E-F8CAB15E3A8D")}',
                    id,
                    'Married',
                    'Married',
                    1,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{maritalStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("7A43DCFF-88CB-4E7A-8CFF-848CE7ED2CA2")}',
                    id,
                    'Divorced',
                    'Divorced',
                    2,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{maritalStatusTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("80F48E50-3AA0-4A68-917A-74A3E9DF8CA1")}',
                    id,
                    'Widowed',
                    'Widowed',
                    3,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{maritalStatusTypeGuid}';
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete DefinedValues first (due to foreign key constraints)
        migrationBuilder.Sql(@"
                DELETE FROM defined_value WHERE guid IN (
                    '618F906C-C33D-4FA3-8AEF-E58CB7B63F1E',
                    '1DAD99D5-41A9-4865-8366-F269902B80A4',
                    '283999EC-7346-42E3-B807-BCE9B2BABB49',
                    '41540783-D9EF-4C70-8F1D-C9E83D91ED5F',
                    '39F491C5-D6AC-4A9B-8AC0-C431CB17D588',
                    'B91BA046-BC1E-400C-B85D-638C1F4E0CE2',
                    '368DD475-242C-49C4-A42C-7278BE690CC2',
                    '407E7E45-7B2E-4FCD-9605-ECB1339F2453',
                    'AA8732FB-2CEA-4C76-8D6D-6AAA2C6A4303',
                    '2CC66D5A-F61C-4B74-9AF9-590A9847C13C',
                    '71F123EF-3F48-4E42-973E-96F265D3D9F6',
                    'F9799606-F5FD-479A-848E-F8CAB15E3A8D',
                    '7A43DCFF-88CB-4E7A-8CFF-848CE7ED2CA2',
                    '80F48E50-3AA0-4A68-917A-74A3E9DF8CA1'
                );
            ");

        // Delete DefinedTypes
        migrationBuilder.Sql(@"
                DELETE FROM defined_type WHERE guid IN (
                    '8522BADD-EBE0-41A9-9F1D-A132A9B9C80A',
                    '2E6540EA-63F0-40FE-BE50-F2A84735E600',
                    '8345DD45-73C6-4F5E-BEBD-B77FC83F18FD',
                    '26A0B2DE-72A1-4558-9B49-4BDE9572F6BC'
                );
            ");
    }
}
