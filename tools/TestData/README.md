# Test Data Generator

CLI tool for generating realistic test data for Koinon RMS development and testing.

## Purpose

Agents and developers need realistic data to:
- Test check-in flows with multiple families
- Verify search performance with large datasets
- Test UI with real-world family structures
- Validate business logic with edge cases

## Usage

```bash
# Build the tool
dotnet build

# Generate small dataset (50 families, 150 people)
dotnet run -- seed --size small

# Generate medium dataset (200 families, 700 people)
dotnet run -- seed --size medium

# Generate large dataset (1000 families, 3500 people)
dotnet run -- seed --size large

# Clear existing data and reseed
dotnet run -- seed --size small --clear

# Custom connection string
dotnet run -- seed --connection "Host=localhost;Port=5432;Database=koinon_test;..."
```

## Dataset Sizes

| Size   | Families | People | Groups | Use Case                |
|--------|----------|--------|--------|-------------------------|
| Small  | 50       | 150    | 10     | Quick testing, demos    |
| Medium | 200      | 700    | 30     | Integration tests       |
| Large  | 1000     | 3500   | 100    | Performance testing     |

## Generated Data

The tool uses [Bogus](https://github.com/bchavez/Bogus) to generate realistic:

### People
- Names (first, middle, last)
- Nicknames (10% of people)
- Email addresses
- Phone numbers (mobile, home, work)
- Birth dates (realistic age distribution)
- Addresses

### Families
- 2-6 members per family
- Realistic roles (adult, child)
- Family addresses
- Primary family assignment

### Groups
- Small groups (8-15 members)
- Classes (15-30 members)
- Serving teams (5-20 members)
- Group types and roles

### Check-in Data
- Check-in configurations for 3 campuses
- 3 service times per campus
- Age-based group assignments
- Historical attendance records

## Data Patterns

### Family Structures
- 35% - Nuclear family (2 adults, 2-3 children)
- 25% - Couple with no children
- 20% - Single parent (1 adult, 1-3 children)
- 10% - Single adult
- 10% - Multigenerational (3+ adults, children)

### Demographics
- Age distribution matches US census data
- Gender distribution: 51% female, 49% male
- Names use culturally diverse name sets
- Phone numbers use valid area codes
- Addresses use real city/state/ZIP combinations

## Performance

| Size   | Generation Time | Database Size |
|--------|-----------------|---------------|
| Small  | ~5 seconds      | ~5 MB         |
| Medium | ~20 seconds     | ~20 MB        |
| Large  | ~2 minutes      | ~100 MB       |

## Docker Integration

Add to `docker-compose.yml` for automatic seeding:

```yaml
services:
  seed-data:
    build:
      context: .
      dockerfile: tools/TestData/Dockerfile
    depends_on:
      - postgres
    environment:
      - ConnectionStrings__KoinonDb=Host=postgres;Port=5432;Database=koinon;Username=koinon;Password=koinon
    command: seed --size small
```

## Implementation Status

⚠️ **Pending Entity Layer**

This tool requires entities from work units:
- [ ] WU-1.2.1: Base entity classes
- [ ] WU-1.2.2: DefinedType/DefinedValue
- [ ] WU-1.2.3: Campus
- [ ] WU-1.2.4: Person (core)
- [ ] WU-1.2.5: PhoneNumber
- [ ] WU-1.2.6: Group/GroupType
- [ ] WU-1.2.7: GroupMember
- [ ] WU-1.2.8: Location
- [ ] WU-1.3.1: DbContext

Once these are complete, implement:
1. `Generators/PersonGenerator.cs` - Person/family data
2. `Generators/GroupGenerator.cs` - Group/membership data
3. `Generators/CheckinGenerator.cs` - Check-in config/attendance
4. `Seeders/DatabaseSeeder.cs` - Database insertion logic

## CI/CD Integration

Used in GitHub Actions for integration tests:

```yaml
- name: Seed test database
  run: |
    dotnet run --project tools/TestData -- seed \
      --size small \
      --connection "${{ env.TEST_DB_CONNECTION }}"
```

## Agent Usage

When agents need test data:
1. Run: `dotnet run --project tools/TestData -- seed --size small`
2. Data is immediately available in database
3. Use for testing services, API endpoints, UI components
