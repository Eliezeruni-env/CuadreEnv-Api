// Usage: node generate-jwt.js <email> <userId> <role> <secret> [companyId]
// Example: node generate-jwt.js admin@cuadre.com 1 Admin cuadreenv_super_secret_jwt_key_2026 1

const jwt = require('jsonwebtoken');
const args = process.argv.slice(2);
if (args.length < 4) {
  console.error('Usage: node generate-jwt.js <email> <userId> <role> <secret> [companyId]');
  process.exit(1);
}
const [email, userId, role, secret, companyId] = args;
const payload = {
  sub: userId.toString(),
  nameid: userId.toString(),
  email: email,
  role: role,
};
if (companyId) payload.CompanyId = parseInt(companyId, 10);

const token = jwt.sign(payload, secret, { algorithm: 'HS256', expiresIn: '8h' });
console.log(token);
