/**
 * k6 Load Testing Script for Mediconnet API
 * 
 * Usage:
 *   k6 run load-test.js
 *   k6 run --vus 100 --duration 1m load-test.js
 * 
 * VUS = Virtual Users
 */

import http from 'k6/http';
import { check, group, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  // Scénario 1: Ramping up
  stages: [
    { duration: '2m', target: 100, name: 'ramping_up' },
    { duration: '5m', target: 100, name: 'stay' },
    { duration: '2m', target: 200, name: 'ramping_down' },
    { duration: '1m', target: 0, name: 'down' },
  ],
  
  // Thresholds pour détecter les problèmes
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'], // 95% < 500ms, 99% < 1s
    http_req_failed: ['rate<0.1'], // Moins de 10% d'erreurs
    checks: ['rate>0.95'], // 95% de vérifications passées
  },
};

export default function () {
  group('API Health Checks', () => {
    let res = http.get(`${BASE_URL}/health`);
    check(res, {
      'health check status is 200': (r) => r.status === 200,
      'health check time < 100ms': (r) => r.timings.duration < 100,
    });
  });

  sleep(1);

  group('Patient API', () => {
    let res = http.get(`${BASE_URL}/api/patient`);
    check(res, {
      'patient list status is 200': (r) => r.status === 200,
      'patient list time < 500ms': (r) => r.timings.duration < 500,
      'response has data': (r) => r.body.length > 0,
    });
  });

  sleep(1);

  group('Consultation API', () => {
    let res = http.get(`${BASE_URL}/api/consultation`);
    check(res, {
      'consultation status is 200': (r) => r.status === 200,
      'consultation time < 500ms': (r) => r.timings.duration < 500,
    });
  });

  sleep(1);

  group('Prescription API', () => {
    let res = http.get(`${BASE_URL}/api/prescription`);
    check(res, {
      'prescription status is 200': (r) => r.status === 200,
      'prescription time < 500ms': (r) => r.timings.duration < 500,
    });
  });

  sleep(1);

  group('Pharmacy API', () => {
    let res = http.get(`${BASE_URL}/api/pharmacy`);
    check(res, {
      'pharmacy status is 200': (r) => r.status === 200,
      'pharmacy time < 500ms': (r) => r.timings.duration < 500,
    });
  });

  sleep(2);
}

/**
 * Scénarios supplémentaires
 * 
 * Pour un spike test:
 *   stages: [
 *     { duration: '10s', target: 100 },
 *     { duration: '1m', target: 1000 },  // Spike
 *     { duration: '10s', target: 100 },
 *   ]
 * 
 * Pour un soak test (endurance):
 *   stages: [
 *     { duration: '5m', target: 100 },
 *     { duration: '30m', target: 100 },  // Maintenir longtemps
 *     { duration: '5m', target: 0 },
 *   ]
 */
