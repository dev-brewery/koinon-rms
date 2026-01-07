using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeedTransactionTypeDefinedType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        // Insert Transaction Type DefinedType
        migrationBuilder.InsertData(
            table: "defined_type",
            columns: new[] { "guid", "name", "description", "category", "is_system", "order", "created_date_time" },
            values: new object[,]
            {
                { Guid.Parse("2AACBE45-9C69-4D47-9F30-DDCE7D39E1B4"), "Transaction Type", "Type of financial transaction", "Giving", true, 0, now }
            });

        // Transaction Type DefinedType GUID
        var transactionTypeGuid = "2AACBE45-9C69-4D47-9F30-DDCE7D39E1B4";

        // Insert DefinedValues for Transaction Type
        // SQL OK: All interpolated values are compile-time constants (GUIDs) and controlled DateTime
        migrationBuilder.Sql($@"
                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("2D607262-52D6-4724-910D-424651F01C8B")}',
                    id,
                    'Contribution',
                    'General donation or contribution',
                    0,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{transactionTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("4B0B5C34-8E8A-4F1E-9D3A-5B7F8E2A3C4D")}',
                    id,
                    'Event Registration',
                    'Payment for event registration',
                    1,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{transactionTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("7C9E2F45-6A1B-4D8E-A2C3-8F7E9B4A5D6C")}',
                    id,
                    'Pledge',
                    'Pledge or commitment',
                    2,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{transactionTypeGuid}';

                INSERT INTO defined_value (guid, defined_type_id, value, description, ""order"", is_active, created_date_time)
                SELECT
                    '{Guid.Parse("9E4D6B78-3C2A-4F5E-B1D7-6A8C9E3F2B5D")}',
                    id,
                    'Refund',
                    'Refund of previous transaction',
                    3,
                    true,
                    '{now:yyyy-MM-dd HH:mm:ss}'
                FROM defined_type WHERE guid = '{transactionTypeGuid}';
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete DefinedValues first (due to foreign key constraints)
        migrationBuilder.Sql(@"
                DELETE FROM defined_value WHERE guid IN (
                    '2D607262-52D6-4724-910D-424651F01C8B',
                    '4B0B5C34-8E8A-4F1E-9D3A-5B7F8E2A3C4D',
                    '7C9E2F45-6A1B-4D8E-A2C3-8F7E9B4A5D6C',
                    '9E4D6B78-3C2A-4F5E-B1D7-6A8C9E3F2B5D'
                );
            ");

        // Delete DefinedType
        migrationBuilder.Sql(@"
                DELETE FROM defined_type WHERE guid = '2AACBE45-9C69-4D47-9F30-DDCE7D39E1B4';
            ");
    }
}
