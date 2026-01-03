using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeedFinancialClaims : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        // Insert SecurityClaim records for financial operations
        migrationBuilder.InsertData(
            table: "security_claim",
            columns: new[] { "guid", "claim_type", "claim_value", "description", "created_date_time" },
            values: new object[,]
            {
                { Guid.Parse("A1B2C3D4-E5F6-4A1B-8C9D-111111111111"), "financial", "view", "View financial data including contributions and batches", now },
                { Guid.Parse("A1B2C3D4-E5F6-4A1B-8C9D-222222222222"), "financial", "edit", "Create and edit contributions and batches", now },
                { Guid.Parse("A1B2C3D4-E5F6-4A1B-8C9D-333333333333"), "financial", "batch.close", "Close contribution batches", now }
            });

        // Insert SecurityRole for Financial Admin
        migrationBuilder.InsertData(
            table: "security_role",
            columns: new[] { "guid", "name", "description", "is_system_role", "is_active", "created_date_time" },
            values: new object[] { Guid.Parse("F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC"), "Financial Admin", "Administrator role for financial operations", true, true, now });

        // Link Financial Admin role to all three financial claims with ALLOW access
        // Using SQL to lookup IDs by GUID
        migrationBuilder.Sql($@"
            INSERT INTO role_security_claim (guid, security_role_id, security_claim_id, allow_or_deny, created_date_time)
            SELECT
                '{Guid.Parse("11111111-1111-4AAA-BBBB-111111111111")}',
                (SELECT id FROM security_role WHERE guid = 'F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC'),
                (SELECT id FROM security_claim WHERE guid = 'A1B2C3D4-E5F6-4A1B-8C9D-111111111111'),
                'A',
                '{now:yyyy-MM-dd HH:mm:ss}'
            WHERE NOT EXISTS (
                SELECT 1 FROM role_security_claim 
                WHERE security_role_id = (SELECT id FROM security_role WHERE guid = 'F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC')
                AND security_claim_id = (SELECT id FROM security_claim WHERE guid = 'A1B2C3D4-E5F6-4A1B-8C9D-111111111111')
            );

            INSERT INTO role_security_claim (guid, security_role_id, security_claim_id, allow_or_deny, created_date_time)
            SELECT
                '{Guid.Parse("22222222-2222-4AAA-BBBB-222222222222")}',
                (SELECT id FROM security_role WHERE guid = 'F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC'),
                (SELECT id FROM security_claim WHERE guid = 'A1B2C3D4-E5F6-4A1B-8C9D-222222222222'),
                'A',
                '{now:yyyy-MM-dd HH:mm:ss}'
            WHERE NOT EXISTS (
                SELECT 1 FROM role_security_claim 
                WHERE security_role_id = (SELECT id FROM security_role WHERE guid = 'F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC')
                AND security_claim_id = (SELECT id FROM security_claim WHERE guid = 'A1B2C3D4-E5F6-4A1B-8C9D-222222222222')
            );

            INSERT INTO role_security_claim (guid, security_role_id, security_claim_id, allow_or_deny, created_date_time)
            SELECT
                '{Guid.Parse("33333333-3333-4AAA-BBBB-333333333333")}',
                (SELECT id FROM security_role WHERE guid = 'F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC'),
                (SELECT id FROM security_claim WHERE guid = 'A1B2C3D4-E5F6-4A1B-8C9D-333333333333'),
                'A',
                '{now:yyyy-MM-dd HH:mm:ss}'
            WHERE NOT EXISTS (
                SELECT 1 FROM role_security_claim 
                WHERE security_role_id = (SELECT id FROM security_role WHERE guid = 'F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC')
                AND security_claim_id = (SELECT id FROM security_claim WHERE guid = 'A1B2C3D4-E5F6-4A1B-8C9D-333333333333')
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete RoleSecurityClaim records first (due to foreign key constraints)
        migrationBuilder.Sql(@"
            DELETE FROM role_security_claim WHERE guid IN (
                '11111111-1111-4AAA-BBBB-111111111111',
                '22222222-2222-4AAA-BBBB-222222222222',
                '33333333-3333-4AAA-BBBB-333333333333'
            );
        ");

        // Delete SecurityRole
        migrationBuilder.Sql(@"
            DELETE FROM security_role WHERE guid = 'F1F1F1F1-AAAA-4444-BBBB-CCCCCCCCCCCC';
        ");

        // Delete SecurityClaim records
        migrationBuilder.Sql(@"
            DELETE FROM security_claim WHERE guid IN (
                'A1B2C3D4-E5F6-4A1B-8C9D-111111111111',
                'A1B2C3D4-E5F6-4A1B-8C9D-222222222222',
                'A1B2C3D4-E5F6-4A1B-8C9D-333333333333'
            );
        ");
    }
}
