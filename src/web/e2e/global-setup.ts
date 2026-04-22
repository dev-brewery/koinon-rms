/**
 * Playwright Global Setup
 *
 * Cleans test-created records before each test run to prevent
 * pagination issues caused by accumulated test artifacts.
 *
 * Seed data IDs are preserved; everything else is deleted.
 */

const SEED_PERSON_IDS = [1,2,3,4,5,6,7,8,9,10,11,12,15,17,18,20,21,22,23];
const SEED_FAMILY_IDS = [1,2];

async function globalSetup() {
  let client;
  try {
    const pg = await import('pg');
    client = new pg.default.Client({
      host: process.env.PGHOST || 'localhost',
      port: Number(process.env.PGPORT || 5432),
      database: process.env.PGDATABASE || 'koinon',
      user: process.env.PGUSER || 'koinon',
      password: process.env.PGPASSWORD || 'koinon',
    });
    await client.connect();

    const personIds = SEED_PERSON_IDS.join(',');
    const familyIds = SEED_FAMILY_IDS.join(',');

    // Clean attendance data so check-in tests start from a known state
    await client.query('DELETE FROM follow_up');
    await client.query('DELETE FROM attendance');
    await client.query('DELETE FROM attendance_occurrence');

    await client.query(`DELETE FROM family_member WHERE family_id NOT IN (${familyIds})`);
    await client.query(`DELETE FROM family WHERE id NOT IN (${familyIds})`);
    await client.query(`DELETE FROM person_alias WHERE person_id NOT IN (${personIds})`);
    await client.query(`DELETE FROM phone_number WHERE person_id NOT IN (${personIds})`);
    await client.query(`DELETE FROM person WHERE id NOT IN (${personIds})`);
  } catch {
    // DB cleanup is best-effort; tests still run if cleanup fails
  } finally {
    if (client) await client.end().catch(() => {});
  }
}

export default globalSetup;
