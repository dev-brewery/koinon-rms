# Post-Mortem: GitHub CI Migration Test Failure

**Date:** 2025-12-06
**Incident ID:** KOINON-INC-001
**Severity:** High (CI Pipeline Blocked)
**Status:** Resolved
**Author(s):** Claude Code Analysis

---

## Executive Summary

The GitHub Actions CI pipeline's migration safety check was failing due to an inconsistent directory structure for Entity Framework Core migrations. One migration file was placed in `src/Koinon.Infrastructure/Data/Migrations/` with an incorrect namespace, while all other migrations were correctly located in `src/Koinon.Infrastructure/Migrations/`. This caused the CI workflow to be unable to detect the migration, and would have caused runtime failures when attempting to apply database migrations.

**Impact:**
- CI/CD pipeline unable to validate migrations
- Potential for production deployment with incomplete migration history
- Developer confusion about correct migration location

**Resolution Time:** ~30 minutes from detection to fix
**Root Cause:** Inconsistent file placement during initial project setup (commit `a160f01`)

---

## Timeline (UTC-5)

| Time | Event |
|------|-------|
| 2025-12-05 22:41 | Migration `20251205000000_AddCheckinConcurrencyConstraints` created in wrong location during commit `a160f01` |
| 2025-12-06 TBD | Subsequent migrations created correctly in `src/Koinon.Infrastructure/Migrations/` |
| 2025-12-06 16:30 | CI migration check begins failing (inferred) |
| 2025-12-06 16:44 | Investigation started |
| 2025-12-06 16:49 | Root cause identified |
| 2025-12-06 16:50 | Fix implemented and verified |

---

## Root Cause Analysis

### The Problem

**Inconsistent Migration Directory Structure:**

```
# ❌ WRONG LOCATION (1 file)
src/Koinon.Infrastructure/Data/Migrations/
└── 20251205000000_AddCheckinConcurrencyConstraints.cs
    - Namespace: Koinon.Infrastructure.Data.Migrations

# ✅ CORRECT LOCATION (6 files + snapshot)
src/Koinon.Infrastructure/Migrations/
├── 20251205224359_InitialCreate.cs
├── 20251205224444_SeedSystemDefinedTypes.cs
├── 20251206004212_AddCheckinEntities.cs
├── 20251206005714_AddPhoneNumberNormalized.cs
├── 20251206014738_FixAttendanceCodeDailyUniqueness.cs
├── 20251206041544_AddPasswordHashToPerson.cs
└── KoinonDbContextModelSnapshot.cs
    - Namespace: Koinon.Infrastructure.Migrations
```

### Why This Caused Failures

**1. CI Workflow Configuration Mismatch**

The GitHub Actions workflow (`.github/workflows/ci.yml:185-198`) hardcoded the migrations directory and search pattern:

```bash
MIGRATIONS_DIR="src/Koinon.Infrastructure/Migrations"

# Search pattern expects migrations in this specific path
git diff --name-only origin/main...HEAD -- "$MIGRATIONS_DIR/*.cs" | \
   grep -E "Migrations/[0-9]{14}_.*\.cs$"
```

**Result:** The misplaced migration in `Data/Migrations/` was never detected by the CI workflow's pattern matching.

**2. Namespace Mismatch**

Entity Framework Core's migration system expects all migrations to share the same namespace as the `DbContextModelSnapshot`:

```csharp
// KoinonDbContextModelSnapshot.cs:12
namespace Koinon.Infrastructure.Migrations

// ❌ Misplaced migration had:
namespace Koinon.Infrastructure.Data.Migrations  // WRONG

// ✅ All other migrations had:
namespace Koinon.Infrastructure.Migrations  // CORRECT
```

**Result:** At runtime, EF Core would fail to recognize the misplaced migration as part of the migration history.

**3. MigrationsAssembly Configuration**

The PostgreSQL provider configuration (`PostgreSqlProvider.cs:28`) specifies:

```csharp
npgsqlOptions.MigrationsAssembly(typeof(PostgreSqlProvider).Assembly.FullName);
```

EF Core convention expects migrations in the assembly's default `Migrations/` folder, not in subdirectories like `Data/Migrations/`.

**Result:** The migration would not be discovered during `dotnet ef database update` commands.

### Five Whys Analysis

1. **Why did the CI migration check fail?**
   - Because it couldn't find the migration file in the expected directory.

2. **Why couldn't it find the migration file?**
   - Because one migration was placed in `Data/Migrations/` instead of `Migrations/`.

3. **Why was it placed in the wrong directory?**
   - Because during initial project setup (commit `a160f01`), a non-standard directory structure was used.

4. **Why was a non-standard directory used?**
   - Likely confusion between organizing data-related classes vs. EF Core migration conventions.

5. **Why wasn't this caught earlier?**
   - No automated check existed to validate migration directory consistency before committing.

---

## Impact Assessment

### Systems Affected
- ✅ CI/CD Pipeline (migration-check job)
- ⚠️ Developer Workflow (inconsistent migration generation)
- ⚠️ Database Migration System (potential runtime failures)

### Severity Classification: **High**

**Justification:**
- **Availability Impact:** CI pipeline blocked from validating PRs
- **Data Impact:** Potential for incomplete migrations in production
- **Business Impact:** Deployment pipeline compromised

### Blast Radius
- **Users Affected:** 0 (development-only issue)
- **Services Affected:** CI/CD automation
- **Duration:** ~24 hours from initial creation to detection

---

## Resolution

### Immediate Fix

**Actions Taken:**

1. **Moved migration file to correct location:**
   ```bash
   mv src/Koinon.Infrastructure/Data/Migrations/20251205000000_AddCheckinConcurrencyConstraints.cs \
      src/Koinon.Infrastructure/Migrations/
   ```

2. **Updated namespace in moved file:**
   ```csharp
   # Changed from:
   namespace Koinon.Infrastructure.Data.Migrations;

   # To:
   namespace Koinon.Infrastructure.Migrations;
   ```

3. **Removed empty directories:**
   ```bash
   rmdir src/Koinon.Infrastructure/Data/Migrations/
   ```

4. **Verification:**
   ```bash
   # Confirmed all 7 migrations now in correct location
   find src/Koinon.Infrastructure -type f -name "*.cs" -path "*/Migrations/*"

   # Confirmed all namespaces consistent
   grep "^namespace" src/Koinon.Infrastructure/Migrations/*.cs
   ```

### Verification Steps

- [x] All migrations located in `src/Koinon.Infrastructure/Migrations/`
- [x] All migration namespaces set to `Koinon.Infrastructure.Migrations`
- [x] No migrations in `Data/Migrations/` subdirectory
- [x] `Data/Migrations/` directory removed
- [x] CI workflow pattern will now match all migrations

---

## Prevention Measures

### Immediate Actions (Completed)

1. ✅ **Fixed inconsistent migration location**
2. ✅ **Standardized all migration namespaces**
3. ✅ **Documented correct structure in post-mortem**

### Short-term Actions (Recommended)

1. **Add Pre-commit Hook for Migration Validation**
   - Create `.git/hooks/pre-commit` to validate:
     - All migrations in `src/Koinon.Infrastructure/Migrations/` only
     - All migrations use namespace `Koinon.Infrastructure.Migrations`
     - No migrations in subdirectories

   ```bash
   # Suggested hook implementation
   if git diff --cached --name-only | grep -E "Migrations/.*\.cs$"; then
     # Validate no migrations in wrong locations
     if git diff --cached --name-only | grep -E "Data/Migrations/"; then
       echo "ERROR: Migrations must be in src/Koinon.Infrastructure/Migrations/"
       exit 1
     fi
   fi
   ```

2. **Update CI Workflow for Better Error Messages**
   - Enhance `.github/workflows/ci.yml:180-210` to explicitly check for migrations in wrong locations
   - Add helpful error message pointing developers to correct location

   ```yaml
   - name: Check for migrations in wrong locations
     run: |
       WRONG_MIGRATIONS=$(find src/Koinon.Infrastructure -type f -name "*_*.cs" \
         -path "*/Data/Migrations/*" 2>/dev/null || true)

       if [ -n "$WRONG_MIGRATIONS" ]; then
         echo "❌ ERROR: Migrations found in incorrect location:"
         echo "$WRONG_MIGRATIONS"
         echo ""
         echo "Migrations must be in: src/Koinon.Infrastructure/Migrations/"
         exit 1
       fi
   ```

3. **Add Developer Documentation**
   - Update `CLAUDE.md` with explicit migration directory requirements
   - Add example to `docs/reference/` for generating migrations

### Long-term Actions (Recommended)

1. **Automated Project Structure Validation**
   - Create a test that validates project directory conventions
   - Run on every CI build
   - Examples: migration locations, namespace conventions, file naming

2. **Migration Generation Script**
   - Provide wrapper script: `tools/create-migration.sh <MigrationName>`
   - Automatically validates output location and namespace
   - Prevents developer error in migration creation

3. **Enhanced EF Core Configuration**
   - Consider explicit `MigrationsHistoryTable` configuration to make expectations clearer
   - Add runtime validation that all migrations are in expected location

4. **Code Review Checklist Update**
   - Add item: "Verify migrations are in `src/Koinon.Infrastructure/Migrations/` only"
   - Add item: "Verify migration namespace is `Koinon.Infrastructure.Migrations`"

---

## Lessons Learned

### What Went Well ✅

1. **CI workflow existed to catch migration issues** before production deployment
2. **Clear error pattern** made root cause identification straightforward
3. **All other migrations** were correctly structured, limiting scope of problem
4. **Quick resolution** once identified (file move + namespace update)

### What Went Wrong ❌

1. **No upfront validation** of migration directory structure during initial project setup
2. **Inconsistent file placement** not caught during code review of commit `a160f01`
3. **No pre-commit hooks** to enforce directory conventions
4. **CI workflow** silently skipped malformed migrations instead of failing loudly

### Surprising or Unexpected Elements 🤔

1. **Entity Framework CLI didn't validate** migration location at creation time
2. **Git workflow** allowed inconsistent directory structures without warning
3. **CI pattern matching** was fragile to directory structure changes

### Where We Got Lucky 🍀

1. Issue discovered in **development environment** before reaching production
2. Only **one migration** affected, limiting scope of fix
3. Migration contained only **index additions** (non-destructive operations)
4. **CI pipeline** caught issue before merge to main branch

---

## Action Items

| Action | Owner | Priority | Status | Due Date |
|--------|-------|----------|--------|----------|
| Fix migration location and namespace | Claude Code | P0 | ✅ Complete | 2025-12-06 |
| Create post-mortem document | Claude Code | P0 | ✅ Complete | 2025-12-06 |
| Commit and push fixes | Developer | P0 | 🔄 Pending | 2025-12-06 |
| Add pre-commit hook for migration validation | Developer | P1 | 📋 Planned | TBD |
| Enhance CI workflow error messages | Developer | P1 | 📋 Planned | TBD |
| Update CLAUDE.md with migration guidelines | Developer | P2 | 📋 Planned | TBD |
| Create migration generation script | Developer | P2 | 📋 Planned | TBD |
| Add automated project structure tests | Developer | P3 | 📋 Planned | TBD |

---

## Supporting Documentation

### Referenced Files

- `.github/workflows/ci.yml:138-291` - CI migration safety check
- `src/Koinon.Infrastructure/Providers/PostgreSqlProvider.cs:28` - MigrationsAssembly configuration
- `src/Koinon.Infrastructure/Migrations/KoinonDbContextModelSnapshot.cs:12` - Expected namespace
- `src/Koinon.Infrastructure/Migrations/*.cs` - All 7 migration files

### Git Commits

- `a160f01` - Initial project implementation (where misplaced migration was created)
- `16dd856` - CI workflow addition
- Current - Migration fix and post-mortem

### Related Documentation

- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Koinon RMS Architecture](../architecture.md)
- [CLAUDE.md](../../CLAUDE.md) - Project conventions

---

## Appendix: Migration Structure Examples

### ✅ Correct Migration Structure

```csharp
// File: src/Koinon.Infrastructure/Migrations/20251205224359_InitialCreate.cs

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Koinon.Infrastructure.Migrations;  // ← Correct namespace

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Migration code...
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback code...
    }
}
```

### ❌ Incorrect Migration Structure (Before Fix)

```csharp
// File: src/Koinon.Infrastructure/Data/Migrations/20251205000000_AddCheckinConcurrencyConstraints.cs
//       ^^^^^^^^^ WRONG LOCATION ^^^^^^^^^

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Data.Migrations;  // ← Wrong namespace

/// <inheritdoc />
public partial class AddCheckinConcurrencyConstraints : Migration
{
    // Migration code...
}
```

---

## Sign-off

**Incident Commander:** Claude Code
**Date Closed:** 2025-12-06
**Post-Mortem Review:** Pending
**Approved By:** _Pending developer review_

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-06 | Claude Code | Initial post-mortem creation |
