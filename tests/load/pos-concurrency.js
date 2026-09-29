import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    concurrent_pos_sales: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 10),
      duration: __ENV.DURATION || '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    checks: ['rate>0.95'],
  },
};

const baseUrl = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/$/, '');
const token = __ENV.JWT;
const cashRegisterId = Number(__ENV.CASH_REGISTER_ID || 1);
const productId = Number(__ENV.PRODUCT_ID || 1);

export default function () {
  if (!token) {
    throw new Error('JWT environment variable is required');
  }

  const key = `load-${__VU}-${__ITER}-${Date.now()}`;
  const payload = JSON.stringify({
    idempotencyKey: key,
    cashRegisterId,
    items: [{ productId, quantity: 1, unitPrice: 1 }],
  });
  const response = http.post(`${baseUrl}/v1/caja/sales`, payload, {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
      'X-Idempotency-Key': key,
    },
  });

  check(response, {
    'sale accepted or explicitly rejected for stock/conflict': (r) =>
      [201, 409, 422].includes(r.status),
    'response is not a server error': (r) => r.status < 500,
  });
}
