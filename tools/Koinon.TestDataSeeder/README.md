# Koinon Test Data Seeder

Deterministic test data seeder for E2E and integration testing. Seeds fixed test data with predictable GUIDs for easy reference in automated tests.

## Features

- Deterministic data with fixed GUIDs for reliable testing
- Database reset capability (truncate all tables)
- Realistic family and group structures
- Check-in group examples with age/grade restrictions

## Usage

### Basic Seeding

```bash
# Seed test data (uses default connection to local docker-compose database)
dotnet run --project tools/Koinon.TestDataSeeder -- seed

# Reset database and seed fresh data
dotnet run --project tools/Koinon.TestDataSeeder -- seed --reset
```

### Custom Connection String

```bash
# Using connection string option (provide your actual PostgreSQL connection string)
dotnet run --project tools/Koinon.TestDataSeeder -- seed --connection "Host=myhost;Port=5432;Database=mydb;Username=myuser;Pwd=\$POSTGRES_PASSWORD"

# Using environment variable (set to your actual PostgreSQL connection string)
export KOINON_CONNECTION_STRING="Host=myhost;Port=5432;Database=mydb;Username=myuser;Pwd=\$POSTGRES_PASSWORD"
dotnet run --project tools/Koinon.TestDataSeeder -- seed
```

### Help

```bash
dotnet run --project tools/Koinon.TestDataSeeder -- --help
dotnet run --project tools/Koinon.TestDataSeeder -- seed --help
```

## Seeded Data

### Families

**Smith Family** (GUID: `11111111-1111-1111-1111-111111111111`)
- John Smith (Adult, GUID: `33333333-3333-3333-3333-333333333333`)
- Jane Smith (Adult, GUID: `44444444-4444-4444-4444-444444444444`)
- Johnny Smith (Child, age 6, GUID: `55555555-5555-5555-5555-555555555555`)
- Jenny Smith (Child, age 4, GUID: `66666666-6666-6666-6666-666666666666`) - Has peanut allergy

**Johnson Family** (GUID: `22222222-2222-2222-2222-222222222222`)
- Bob Johnson (Adult, GUID: `77777777-7777-7777-7777-777777777777`)
- Barbara Johnson (Adult, GUID: `88888888-8888-8888-8888-888888888888`)
- Billy Johnson (Child, age 5, GUID: `99999999-9999-9999-9999-999999999999`)

### Check-in Groups

- **Nursery** (GUID: `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`)
  - Capacity: 15
  - Ages: 0-2 years (0-24 months)

- **Preschool** (GUID: `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb`)
  - Capacity: 20
  - Ages: 3-5 years (36-71 months)

- **Elementary** (GUID: `cccccccc-cccc-cccc-cccc-cccccccccccc`)
  - Capacity: 30
  - Grades: K-5

### Schedules

- **Sunday 9:00 AM** (GUID: `dddddddd-dddd-dddd-dddd-dddddddddddd`)
  - Check-in opens: 60 minutes before
  - Check-in closes: 30 minutes after

- **Sunday 11:00 AM** (GUID: `eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee`)
  - Check-in opens: 60 minutes before
  - Check-in closes: 30 minutes after

- **Wednesday 7:00 PM** (GUID: `ffffffff-ffff-ffff-ffff-ffffffffffff`)
  - Check-in opens: 30 minutes before
  - Check-in closes: 15 minutes after

## Using in Tests

### E2E Tests (Playwright/Cypress)

Reference the TypeScript fixtures at `src/web/e2e/fixtures/test-data.ts`:

```typescript
import { testData } from './fixtures/test-data';

// Use in tests
await page.fill('#search', testData.people.johnSmith.fullName);
expect(response.guid).toBe(testData.families.smith.guid);
```

### Integration Tests (.NET)

```csharp
// Use the known GUIDs in your tests
var smithFamilyGuid = new Guid("11111111-1111-1111-1111-111111111111");
var johnSmithGuid = new Guid("33333333-3333-3333-3333-333333333333");

var family = await context.Groups.FirstOrDefaultAsync(g => g.Guid == smithFamilyGuid);
Assert.NotNull(family);
```

## Integration with CI/CD

Add to your test setup scripts:

```bash
# Reset and seed before E2E tests
dotnet run --project tools/Koinon.TestDataSeeder -- seed --reset
```

## Security Note

The default connection string matches the credentials in `docker-compose.yml` for local development. These are intentionally simple credentials for the local development environment only.

**Never use these credentials in production.**

For other environments, always provide connection strings via:
- `--connection` CLI option
- `KOINON_CONNECTION_STRING` environment variable
