import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const apptTrend = new Trend('appointments_response_time');

export const options = {
  scenarios: {
    steady: {
      executor: 'constant-vus',
      vus: 20,
      duration: '1m',
    },
    spike: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50  },
        { duration: '1m',  target: 100 },
        { duration: '30s', target: 0   },
      ],
      startTime: '70s',
    },
  },
  thresholds: {
    http_req_duration:         ['p(95)<500', 'p(99)<1000'],
    http_req_failed:           ['rate<0.01'],
    appointments_response_time:['p(95)<400'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

const DATES = Array.from({ length: 30 }, (_, i) => {
  const d = new Date();
  d.setDate(d.getDate() + i + 1);
  return d.toISOString().split('T')[0];
});

export default function () {
  const date = DATES[Math.floor(Math.random() * DATES.length)];
  const res  = http.get(`${BASE_URL}/api/appointments?date=${date}`);

  apptTrend.add(res.timings.duration);

  const ok = check(res, {
    'status 200':       (r) => r.status === 200,
    'duration < 1s':    (r) => r.timings.duration < 1000,
    'valid JSON':       (r) => { try { JSON.parse(r.body); return true; }
                                  catch { return false; } },
  });
  errorRate.add(!ok);

  sleep(Math.random() * 1 + 0.5);
}