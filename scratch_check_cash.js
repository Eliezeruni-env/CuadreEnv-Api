const { Client } = require('pg');
const client = new Client({
  host: 'aws-0-us-east-1.pooler.supabase.com',
  port: 5432,
  database: 'postgres',
  user: 'postgres.uhgmavivdoggwanimhqa',
  password: '!pk%kPbm+gU27_,',
  ssl: { rejectUnauthorized: false }
});
async function run() {
  await client.connect();
  const regs = await client.query('SELECT "Id", "Name", "Status", "CompanyId", "OpenedByUserId", "CreationDate", "InitialAmount" FROM "CashRegisters" ORDER BY "Id" DESC LIMIT 10;');
  console.log('CashRegisters:', regs.rows);

  const sessions = await client.query('SELECT "Id", "CashRegisterId", "CashierUserId", "Status", "CompanyId", "OpenedAt", "ClosedAt" FROM "CashSessions" ORDER BY "Id" DESC LIMIT 10;');
  console.log('CashSessions:', sessions.rows);

  await client.end();
}
run().catch(console.error);

