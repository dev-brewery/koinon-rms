using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeedTransactionSourceDefinedType : Migration
{
    // Parent DefinedType
    private const string TransactionSourceDefinedTypeGuid = "3D4E8F7A-2B1C-4E6A-9F8B-1A2C3D4E5F60";

    // DefinedValues (Transaction Source enum values)
    private const string ManualEntrySourceGuid = "3D4E8F7A-2B1C-4E6A-9F8B-1A2C3D4E5F61";
    private const string OnlineSourceGuid = "3D4E8F7A-2B1C-4E6A-9F8B-1A2C3D4E5F62";
    private const string MobileSourceGuid = "3D4E8F7A-2B1C-4E6A-9F8B-1A2C3D4E5F63";
    private const string MailSourceGuid = "3D4E8F7A-2B1C-4E6A-9F8B-1A2C3D4E5F64";
    private const string KioskSourceGuid = "3D4E8F7A-2B1C-4E6A-9F8B-1A2C3D4E5F65";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        // Insert the parent "Transaction Source" DefinedType.
        // BatchDonationEntryService looks up DefinedValues where DefinedType.Name == "Transaction Source"
        // to resolve the SourceTypeValueId on every contribution (e.g. "Manual Entry" for admin batch entry,
        // "Kiosk" for kiosk giving, etc.). Without this seed, AddContribution always fails with a 422.
        migrationBuilder.InsertData(
            table: "defined_type",
            columns: new[] { "guid", "name", "description", "category", "is_system", "order", "created_date_time" },
            values: new object[,]
            {
                { Guid.Parse(TransactionSourceDefinedTypeGuid), "Transaction Source", "Origin channel of a financial transaction (e.g. Manual Entry, Online, Kiosk)", "Giving", true, 0, now }
            });

        // Insert DefinedValues for Transaction Source.
        // SQL OK: All interpolated values are compile-time constants (GUIDs) and controlled DateTime.
        migrationBuilder.Sql($@"
                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse(ManualEntrySourceGuid)}',
                    id,
                    'Manual Entry',
                    'Contribution entered manually by staff via admin batch entry',
                    0,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{TransactionSourceDefinedTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse(OnlineSourceGuid)}',
                    id,
                    'Online',
                    'Contribution given through the online giving portal',
                    1,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{TransactionSourceDefinedTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse(MobileSourceGuid)}',
                    id,
                    'Mobile',
                    'Contribution given through a mobile device or app',
                    2,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{TransactionSourceDefinedTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse(MailSourceGuid)}',
                    id,
                    'Mail',
                    'Contribution received by mail',
                    3,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{TransactionSourceDefinedTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse(KioskSourceGuid)}',
                    id,
                    'Kiosk',
                    'Contribution given at a physical kiosk',
                    4,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{TransactionSourceDefinedTypeGuid}';
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete DefinedValues first (foreign key constraint).
        migrationBuilder.Sql($@"
                DELETE FROM defined_value WHERE guid IN (
                    '{ManualEntrySourceGuid}',
                    '{OnlineSourceGuid}',
                    '{MobileSourceGuid}',
                    '{MailSourceGuid}',
                    '{KioskSourceGuid}'
                );
            ");

        // Delete parent DefinedType.
        migrationBuilder.Sql($@"
                DELETE FROM defined_type WHERE guid = '{TransactionSourceDefinedTypeGuid}';
            ");
    }
}
