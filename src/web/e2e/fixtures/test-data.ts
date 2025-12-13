/**
 * Test Data Fixtures
 *
 * Matches the seeded data from tools/Koinon.TestDataSeeder
 * Use these constants in E2E tests for reliable, deterministic testing.
 *
 * To seed this data into the database:
 *   dotnet run --project tools/Koinon.TestDataSeeder -- seed --reset
 */

export const testData = {
  families: {
    smith: {
      guid: '11111111-1111-1111-1111-111111111111',
      name: 'Smith Family',
      members: [
        '33333333-3333-3333-3333-333333333333', // John
        '44444444-4444-4444-4444-444444444444', // Jane
        '55555555-5555-5555-5555-555555555555', // Johnny
        '66666666-6666-6666-6666-666666666666', // Jenny
      ],
    },
    johnson: {
      guid: '22222222-2222-2222-2222-222222222222',
      name: 'Johnson Family',
      members: [
        '77777777-7777-7777-7777-777777777777', // Bob
        '88888888-8888-8888-8888-888888888888', // Barbara
        '99999999-9999-9999-9999-999999999999', // Billy
      ],
    },
  },

  people: {
    johnSmith: {
      guid: '33333333-3333-3333-3333-333333333333',
      firstName: 'John',
      lastName: 'Smith',
      fullName: 'John Smith',
      email: 'john.smith@example.com',
      age: new Date().getFullYear() - 1985,
      birthDate: {
        year: 1985,
        month: 6,
        day: 15,
      },
    },
    janeSmith: {
      guid: '44444444-4444-4444-4444-444444444444',
      firstName: 'Jane',
      lastName: 'Smith',
      fullName: 'Jane Smith',
      email: 'jane.smith@example.com',
      age: new Date().getFullYear() - 1987,
      birthDate: {
        year: 1987,
        month: 8,
        day: 22,
      },
    },
    johnnySmith: {
      guid: '55555555-5555-5555-5555-555555555555',
      firstName: 'Johnny',
      lastName: 'Smith',
      fullName: 'Johnny Smith',
      age: 6,
      birthDate: {
        year: new Date().getFullYear() - 6,
        month: 3,
        day: 10,
      },
    },
    jennySmith: {
      guid: '66666666-6666-6666-6666-666666666666',
      firstName: 'Jenny',
      lastName: 'Smith',
      fullName: 'Jenny Smith',
      age: 4,
      allergies: 'Peanuts',
      hasCriticalAllergies: true,
      birthDate: {
        year: new Date().getFullYear() - 4,
        month: 11,
        day: 5,
      },
    },
    bobJohnson: {
      guid: '77777777-7777-7777-7777-777777777777',
      firstName: 'Bob',
      lastName: 'Johnson',
      fullName: 'Bob Johnson',
      email: 'bob.johnson@example.com',
      age: new Date().getFullYear() - 1980,
      birthDate: {
        year: 1980,
        month: 2,
        day: 14,
      },
    },
    barbaraJohnson: {
      guid: '88888888-8888-8888-8888-888888888888',
      firstName: 'Barbara',
      lastName: 'Johnson',
      fullName: 'Barbara Johnson',
      email: 'barbara.johnson@example.com',
      age: new Date().getFullYear() - 1982,
      birthDate: {
        year: 1982,
        month: 9,
        day: 30,
      },
    },
    billyJohnson: {
      guid: '99999999-9999-9999-9999-999999999999',
      firstName: 'Billy',
      lastName: 'Johnson',
      fullName: 'Billy Johnson',
      age: 5,
      birthDate: {
        year: new Date().getFullYear() - 5,
        month: 7,
        day: 18,
      },
    },
  },

  groups: {
    nursery: {
      guid: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Nursery',
      description: 'Nursery for infants and toddlers',
      capacity: 15,
      minAgeMonths: 0,
      maxAgeMonths: 24,
    },
    preschool: {
      guid: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      name: 'Preschool',
      description: 'Preschool ministry for ages 3-5',
      capacity: 20,
      minAgeMonths: 36,
      maxAgeMonths: 71,
    },
    elementary: {
      guid: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      name: 'Elementary',
      description: 'Elementary ministry for grades K-5',
      capacity: 30,
      minGrade: 0,
      maxGrade: 5,
    },
  },

  schedules: {
    sunday9am: {
      guid: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
      name: 'Sunday 9:00 AM',
      dayOfWeek: 0, // Sunday
      hour: 9,
      minute: 0,
      checkInStartOffsetMinutes: 60,
      checkInEndOffsetMinutes: 30,
    },
    sunday11am: {
      guid: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
      name: 'Sunday 11:00 AM',
      dayOfWeek: 0, // Sunday
      hour: 11,
      minute: 0,
      checkInStartOffsetMinutes: 60,
      checkInEndOffsetMinutes: 30,
    },
    wednesday7pm: {
      guid: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
      name: 'Wednesday 7:00 PM',
      dayOfWeek: 3, // Wednesday
      hour: 19,
      minute: 0,
      checkInStartOffsetMinutes: 30,
      checkInEndOffsetMinutes: 15,
    },
  },
} as const;

export type TestData = typeof testData;
