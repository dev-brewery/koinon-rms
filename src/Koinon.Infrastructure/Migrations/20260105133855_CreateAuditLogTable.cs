using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class CreateAuditLogTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS audit_log (
                id SERIAL PRIMARY KEY,
                guid UUID NOT NULL,
                created_date_time TIMESTAMPTZ NOT NULL,
                modified_date_time TIMESTAMPTZ,
                created_by_person_alias_id INTEGER,
                modified_by_person_alias_id INTEGER,
                person_id INTEGER REFERENCES person(id) ON DELETE SET NULL,
                action_type INTEGER NOT NULL,
                entity_type VARCHAR(100) NOT NULL,
                entity_id_key VARCHAR(20) NOT NULL,
                timestamp TIMESTAMPTZ NOT NULL,
                old_values JSONB,
                new_values JSONB,
                changed_properties JSONB,
                ip_address VARCHAR(45),
                user_agent VARCHAR(500),
                additional_info TEXT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS uix_audit_log_guid ON audit_log(guid);
            CREATE INDEX IF NOT EXISTS ix_audit_log_timestamp ON audit_log(timestamp);
            CREATE INDEX IF NOT EXISTS ix_audit_log_entity_type_entity_id_key ON audit_log(entity_type, entity_id_key);
            CREATE INDEX IF NOT EXISTS ix_audit_log_person_id ON audit_log(person_id);
            CREATE INDEX IF NOT EXISTS ix_audit_log_action_type ON audit_log(action_type);
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS audit_log;");
    }
}
