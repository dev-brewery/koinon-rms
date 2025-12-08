# EF Core Migration Guidelines

This document provides best practices for creating, modifying, and managing Entity Framework Core migrations in the Koinon RMS project.

## Table of Contents
- [Core Principles](#core-principles)
- [Migration Safety Rules](#migration-safety-rules)
- [Index Naming Conventions](#index-naming-conventions)
- [Common Migration Patterns](#common-migration-patterns)
- [Shadow Property Prevention](#shadow-property-prevention)
- [Migration Testing](#migration-testing)
- [Troubleshooting](#troubleshooting)

---

## Core Principles

### Never Modify Applied Migrations
Once a migration has been applied to any environment beyond your local development database, **NEVER modify it**. This includes:
- Production databases
- Staging/QA environments
- Other developers' local databases
- CI/CD pipelines

**Rationale:** EF Core uses a checksum to verify migration integrity. Modifying an applied migration will cause:
- Migration history corruption
- Checksum mismatches
- Inability to apply future migrations
- Potential data loss or inconsistency

### Creating New Migrations Instead
If you need to change something in an applied migration:
1. Create a NEW migration with the corrective changes
2. Document why the change is needed in migration comments
3. Never delete or modify the original migration

---

## Migration Safety Rules

### 1. Safe Operations (Metadata-Only)
These operations change metadata without affecting data or indexes:

**RenameIndex:**
```csharp
migrationBuilder.RenameIndex(
    name: "IX_old_name",
    table: "table_name",
    newName: "ix_new_name");
```
- Changes only the index name in system catalogs
- No impact on index structure or query performance
- No data movement or locking
- Safe to run on production during business hours

**RenameColumn:**
```csharp
migrationBuilder.RenameColumn(
    name: "OldName",
    table: "table_name",
    newName: "new_name");
```
- PostgreSQL renames the column in system catalogs only
- No data is copied or moved
- Minimal locking (brief table-level lock)
- Generally safe on production with caution

**RenameTable:**
```csharp
migrationBuilder.RenameTable(
    name: "old_table",
    newName: "new_table");
```
- Renames table in system catalogs only
- No data movement
- Brief table-level lock required
- Safe but coordinate with application deployments

### 2. Index Operations Requiring Caution

**CreateIndex:**
```csharp
migrationBuilder.CreateIndex(
    name: "ix_table_column",
    table: "table_name",
    column: "column_name");
```
- Locks table for duration of index creation
- For large tables, use `CONCURRENTLY` option:
```csharp
migrationBuilder.Sql(
    "CREATE INDEX CONCURRENTLY ix_table_column ON table_name (column_name);");
```
- Concurrent index creation takes longer but doesn't block writes
- Plan for maintenance windows on large tables

**DropIndex with Existence Check:**
```csharp
// ALWAYS use IF EXISTS for safety
migrationBuilder.Sql("DROP INDEX IF EXISTS \"index_name\";");
```
- Use SQL command instead of `DropIndex()` method for safety
- `IF EXISTS` prevents errors if index doesn't exist
- Especially important for shadow property indexes that may vary

### 3. Dangerous Operations

**DropColumn:**
- Irreversible data loss
- Requires application downtime (queries will fail if column is still referenced)
- Use multi-step approach:
  1. Deploy code that doesn't reference column
  2. After deployment is stable, drop column in separate migration
  3. Never combine with other changes

**DropTable:**
- Complete data loss
- Requires coordination with all services
- Document data export/backup procedures

**AlterColumn (changing type):**
- May require data conversion
- Can cause data loss (e.g., string to int)
- PostgreSQL may need to rewrite entire table
- Test thoroughly with production-like data volumes

---

## Index Naming Conventions

All indexes in Koinon RMS follow snake_case naming:

| Index Type | Pattern | Example |
|------------|---------|---------|
| Primary Key | `{table}_pkey` | `person_pkey` (auto-generated) |
| Foreign Key | `ix_{table}_{column}` | `ix_group_member_person_id` |
| Unique | `uix_{table}_{column(s)}` | `uix_person_guid` |
| Composite | `ix_{table}_{col1}_{col2}` | `ix_person_last_name_first_name` |
| Filtered | `ix_{table}_{column}` + HasFilter | `ix_person_email` (where email IS NOT NULL) |

### Convention Rules

1. **Always use snake_case** (lowercase with underscores)
2. **Prefix:**
   - `ix_` for regular indexes
   - `uix_` for unique indexes
   - No prefix for system-generated constraints (pkey, fkey)
3. **Table name comes first:** `ix_person_...` not `ix_person_id_person`
4. **Column names in order:** For composite indexes, list columns left to right
5. **Abbreviated if needed:** For very long names, abbreviate logically but remain clear

### Using HasDatabaseName()

Always explicitly set index names in entity configurations:

```csharp
// CORRECT: Explicit naming
builder.HasIndex(p => p.PersonId)
    .HasDatabaseName("ix_group_member_person_id");

// WRONG: EF generates PascalCase name
builder.HasIndex(p => p.PersonId);
// Results in: IX_group_member_PersonId (inconsistent)
```

---

## Common Migration Patterns

### Adding a New Entity with Foreign Keys

```csharp
public class AddNewEntityConfiguration : IEntityTypeConfiguration<NewEntity>
{
    public void Configure(EntityTypeBuilder<NewEntity> builder)
    {
        // Table
        builder.ToTable("new_entity");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        // Foreign key with explicit index name
        builder.Property(e => e.ParentId)
            .HasColumnName("parent_id")
            .IsRequired();

        builder.HasIndex(e => e.ParentId)
            .HasDatabaseName("ix_new_entity_parent_id");

        // Navigation property
        builder.HasOne(e => e.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Standardizing Index Names (Safe Migration)

```csharp
public partial class StandardizeIndexNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Document why this is safe
        // MIGRATION SAFETY: RenameIndex is metadata-only, no data or structure changes

        // Remove spurious shadow property indexes
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_table_ShadowProp1\";");

        // Rename to snake_case
        migrationBuilder.RenameIndex(
            name: "IX_table_ColumnId",
            table: "table",
            newName: "ix_table_column_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Always implement Down() for rollback capability
        migrationBuilder.RenameIndex(
            name: "ix_table_column_id",
            table: "table",
            newName: "IX_table_ColumnId");
    }
}
```

---

## Shadow Property Prevention

### What Are Shadow Properties?

Shadow properties are properties that EF Core tracks in its model but don't exist as CLR properties on the entity class. EF Core automatically creates shadow properties (and indexes on them) when it detects a navigation property without a corresponding foreign key property.

### Root Cause

The issue occurs when:
1. An entity has a **navigation property** (e.g., `public Campus? Campus { get; set; }`)
2. The **foreign key property** exists (e.g., `public int? CampusId { get; set; }`)
3. BUT the configuration doesn't properly link them via `HasForeignKey()`

### Example: Group Entity Shadow Property

**Problem Configuration (Missing HasForeignKey):**
```csharp
// Group.cs - Domain Entity
public class Group : Entity
{
    public int? CampusId { get; set; }  // FK property exists
    public virtual Campus? Campus { get; set; }  // Navigation property exists
}

// GroupConfiguration.cs - MISSING link
public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(g => g.CampusId).HasColumnName("campus_id");
        builder.HasIndex(g => g.CampusId).HasDatabaseName("ix_group_campus_id");

        // Navigation defined BUT missing HasForeignKey
        builder.HasOne(g => g.Campus)
            .WithMany()
            // MISSING: .HasForeignKey(g => g.CampusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**What EF Core Does:**
1. Sees `Campus` navigation property
2. Can't find explicit foreign key mapping
3. Creates shadow property `CampusId1` to store the relationship
4. Generates index `IX_group_CampusId1` on the shadow property
5. Your actual `CampusId` property becomes disconnected from the navigation

**Correct Configuration:**
```csharp
public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(g => g.CampusId).HasColumnName("campus_id");
        builder.HasIndex(g => g.CampusId).HasDatabaseName("ix_group_campus_id");

        // Properly link navigation to FK property
        builder.HasOne(g => g.Campus)
            .WithMany()
            .HasForeignKey(g => g.CampusId)  // CRITICAL: Links Campus navigation to CampusId property
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Prevention Checklist

For every navigation property, ensure:

- [ ] Foreign key property exists on the entity class
- [ ] Foreign key property is configured with `HasColumnName()`
- [ ] Navigation property is configured with `HasOne()` or `HasMany()`
- [ ] **`HasForeignKey()` explicitly links navigation to FK property**
- [ ] Index on foreign key column is named with `HasDatabaseName()`

### Detection

Run this after adding a new migration to check for shadow properties:

```bash
# Generate migration
dotnet ef migrations add YourMigrationName -p src/Koinon.Infrastructure -s src/Koinon.Api

# Check for shadow property indicators in the generated migration:
grep -i "CampusId1\|PersonId1\|.*Id1" src/Koinon.Infrastructure/Migrations/*.cs
```

If you find references to properties ending in `1` (like `CampusId1`), you have a shadow property issue.

---

## Migration Testing

### Local Testing Process

1. **Create migration:**
```bash
dotnet ef migrations add DescriptiveName -p src/Koinon.Infrastructure -s src/Koinon.Api
```

2. **Review generated code:**
   - Check index naming (snake_case)
   - Look for shadow properties (property names ending in `1`)
   - Verify Up() and Down() methods are reversible
   - Add safety comments for non-obvious operations

3. **Apply migration locally:**
```bash
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api
```

4. **Verify database state:**
```sql
-- Check index names
SELECT schemaname, tablename, indexname
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

-- Check for unexpected columns (shadow properties)
SELECT table_name, column_name
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name LIKE '%1';  -- Shadow props often end in 1
```

5. **Test rollback:**
```bash
# Roll back one migration
dotnet ef database update PreviousMigrationName -p src/Koinon.Infrastructure -s src/Koinon.Api

# Re-apply to confirm idempotency
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api
```

6. **Run tests:**
```bash
dotnet test
```

### CI/CD Considerations

- Migrations run automatically in CI/CD pipeline against test database
- Never allow migrations to run automatically in production
- Use migration scripts for production deployments:
```bash
dotnet ef migrations script -p src/Koinon.Infrastructure -s src/Koinon.Api -o migration.sql
```

---

## Troubleshooting

### "The model backing the context has changed"

**Cause:** Migration files don't match current model
**Fix:**
1. Ensure latest migration is applied: `dotnet ef database update`
2. If error persists, model changed without migration
3. Create new migration: `dotnet ef migrations add FixModelChanges`

### "Migration has already been applied"

**Cause:** Trying to modify a migration that's in the `__EFMigrationsHistory` table
**Fix:** Create a new migration with changes instead of modifying the existing one

### Index Name Collision

**Cause:** Two entities trying to create indexes with the same name
**Fix:** Use more specific naming: `ix_{table}_{column}` not just `ix_{column}`

### Shadow Property Detected After Migration

**Cause:** Navigation property without `HasForeignKey()` configuration
**Fix:**
1. Update entity configuration to include `HasForeignKey()`
2. Create new migration to drop shadow property index
3. Use `DROP INDEX IF EXISTS` for safety:
```csharp
migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_table_ShadowProp1\";");
```

### Foreign Key Constraint Violation on Migration

**Cause:** Data exists that violates new constraint
**Fix:**
1. Write data migration script to clean up invalid data first
2. Separate data fix from schema change (two migrations)
3. Example:
```csharp
// Migration 1: Fix data
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        UPDATE table_name
        SET foreign_key_id = NULL
        WHERE foreign_key_id NOT IN (SELECT id FROM referenced_table);
    ");
}

// Migration 2: Add constraint
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddForeignKey(
        name: "fk_table_foreign_key_id",
        table: "table_name",
        column: "foreign_key_id",
        principalTable: "referenced_table",
        principalColumn: "id");
}
```

---

## Best Practices Summary

1. **Never modify applied migrations** - Create new ones instead
2. **Always use explicit index naming** with `HasDatabaseName()`
3. **Follow snake_case convention** for all database identifiers
4. **Link all navigation properties** with `HasForeignKey()`
5. **Use IF EXISTS for drops** to handle edge cases gracefully
6. **Document non-obvious operations** with comments in migration code
7. **Test Up() and Down()** to ensure reversibility
8. **Check for shadow properties** after generating each migration
9. **Separate schema and data changes** when possible
10. **Review generated SQL** before applying to production

---

## Additional Resources

- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Index Documentation](https://www.postgresql.org/docs/current/indexes.html)
- [EF Core Shadow Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
- Project-specific: `docs/reference/entity-mappings.md`
- Project-specific: `CLAUDE.md` - Database conventions section
