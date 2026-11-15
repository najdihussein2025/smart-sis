// T76 load test for the Smart SIS API. Simulated data only (SR-23).
//
//   cd tools/load && npm install
//   node loadtest.mjs --base http://127.0.0.1:5080 --users 60 --connections 25 --duration 15 --out results/after.json
//
// 1. Logs in as the seeded admin (1001) and creates --users simulated students (UniversityId 4100001…) through the
//    admin API (409 = already there), then logs them all in.
// 2. Seat race: creates a section with --race-capacity seats in the current term and fires one enroll per student at
//    the same moment. Exactly `capacity` must succeed and the section must report seatsTaken == capacity (rule E6).
// 3. For each endpoint: autocannon with --connections keep-alive connections for --duration seconds; every request uses
//    a random student's token (admin lists use the admin token). Records p50/p95/p99, req/s and non-2xx.
// 4. Prints a Markdown table and writes the raw numbers to --out.
//
// Mutates the database (users, a section, enrollments): run it on a throw-away dev stack.
// Use 127.0.0.1, not localhost: on Windows "localhost" tries IPv6 first and adds seconds to new connections.

import autocannon from 'autocannon';
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname } from 'node:path';
import os from 'node:os';

const args = Object.fromEntries(
  process.argv.slice(2).reduce((acc, a, i, all) => (a.startsWith('--') ? [...acc, [a.slice(2), all[i + 1]]] : acc), []),
);
const BASE = args.base ?? 'http://127.0.0.1:5080';
const USERS = Number(args.users ?? 60);
const CONNECTIONS = Number(args.connections ?? 25);
const DURATION = Number(args.duration ?? 15);
const RACE_CAPACITY = Number(args['race-capacity'] ?? 5);
const OUT = args.out ?? 'results/run.json';
const ONLY = (args.only ?? '').split(',').filter(Boolean);
const PASSWORD = 'Pass@1234';
const FIRST_ID = 4100001;

async function api(method, path, token, body) {
  const res = await fetch(BASE + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  return { status: res.status, json: text ? safeJson(text) : null, text };
}
const safeJson = (t) => { try { return JSON.parse(t); } catch { return null; } };

async function login(uid) {
  const r = await api('POST', '/api/auth/login', null, { universityId: uid, password: PASSWORD });
  if (r.status !== 200) throw new Error(`login ${uid}: ${r.status}`);
  return r.json.accessToken;
}

async function inBatches(items, size, fn) {
  const out = [];
  for (let i = 0; i < items.length; i += size) out.push(...(await Promise.all(items.slice(i, i + size).map(fn))));
  return out;
}

async function ensureStudents(admin) {
  const lk = (await api('GET', '/api/admin/lookups', admin)).json;
  const ids = Array.from({ length: USERS }, (_, i) => FIRST_ID + i);
  await inBatches(ids, 8, async (uid) => {
    const r = await api('POST', '/api/admin/users', admin, {
      universityId: uid, email: `load${uid}@smartsis.local`, fullName: `Load Student ${uid}`, role: 'Student',
      initialPassword: PASSWORD,
      student: { majorId: lk.majors[0].id, campusId: lk.campuses[0].id, enrollmentDate: new Date().toISOString().slice(0, 10), maxCreditsOverride: null },
    });
    if (![200, 201, 409].includes(r.status)) throw new Error(`create ${uid}: ${r.status} ${r.text.slice(0, 200)}`);
  });
  return ids;
}

const pct = (sorted, p) => sorted[Math.min(sorted.length - 1, Math.max(0, Math.round((p / 100) * (sorted.length - 1))))];

async function seatRace(admin, tokens) {
  const term = (await api('GET', '/api/terms/current', admin)).json;
  const lk = (await api('GET', '/api/admin/lookups', admin)).json;
  const courses = (await api('GET', '/api/admin/courses?pageSize=100', admin)).json.items;
  let course;
  for (const c of courses) {
    const d = (await api('GET', `/api/admin/courses/${c.id}`, admin)).json;
    if (d.prerequisites.length === 0) { course = d; break; }
  }
  // A slot no other section uses: try rooms x late Saturday hours until the room-clash check passes.
  let created;
  for (let attempt = 0; attempt < 40; attempt++) {
    const code = 'R' + Math.floor(1000 + Math.random() * 9000);
    const room = lk.rooms[attempt % lk.rooms.length].id;
    const hour = 7 + Math.floor(attempt / lk.rooms.length);
    created = await api('POST', '/api/admin/sections', admin, {
      courseId: course.id, termId: term.id, sectionCode: code, instructorId: null, capacity: RACE_CAPACITY,
      meetings: [{ id: null, dayOfWeek: 6, startTime: `${String(hour).padStart(2, '0')}:00:00`, endTime: `${String(hour).padStart(2, '0')}:50:00`, roomId: room, type: 'Lecture' }],
    });
    if (created.status !== 409) break;
  }
  if (created.status !== 201 && created.status !== 200) throw new Error(`section: ${created.status} ${created.text}`);
  const code = created.json.sectionCode;
  const sectionId = created.json.id;

  const t0 = performance.now();
  const results = await Promise.all(tokens.map(async (tok) => {
    const s = performance.now();
    const r = await api('POST', '/api/enrollment/enroll', tok, { sectionIds: [sectionId] });
    const ok = r.status === 200 && r.json.enrolledCount === 1;
    return { status: r.status, ok, ms: performance.now() - s, code: ok ? 'ok' : (r.json?.results?.[0]?.code ?? `http_${r.status}`) };
  }));
  const wall = performance.now() - t0;
  const detail = (await api('GET', `/api/admin/sections/${sectionId}`, admin)).json;
  const outcomes = {};
  for (const r of results) outcomes[r.code] = (outcomes[r.code] ?? 0) + 1;
  const lat = results.map((r) => r.ms).sort((a, b) => a - b);
  return {
    section: `${course.code} ${code}`, capacity: RACE_CAPACITY, attempts: results.length,
    succeeded: results.filter((r) => r.ok).length, seatsTaken: detail.seatsTaken,
    http5xx: results.filter((r) => r.status >= 500).length, outcomes,
    p50: pct(lat, 50), p95: pct(lat, 95), max: lat.at(-1), wallMs: wall,
  };
}

function run(name, opts) {
  return new Promise((resolve, reject) => {
    autocannon({ connections: CONNECTIONS, duration: DURATION, ...opts }, (err, r) => (err ? reject(err) : resolve(r)));
  }).then((r) => ({
    endpoint: name, connections: opts.connections ?? CONNECTIONS, requests: r.requests.total,
    rps: r.requests.average, p50: r.latency.p50, p90: r.latency.p90, p99: r.latency.p99, max: r.latency.max,
    non2xx: r.non2xx, errors: r.errors + r.timeouts,
    errorPct: (100 * (r.non2xx + r.errors + r.timeouts)) / Math.max(1, r.requests.total),
  }));
}

// autocannon reports p50/p90/p97.5/p99; p95 is not in its fixed set, so we report p90 and p99 alongside p50.
async function main() {
  const admin = await login(1001);
  const ids = await ensureStudents(admin);
  const students = await inBatches(ids, 8, login);
  const seeded = await inBatches([3001, 3002, 3003, 3004, 3005, 3006], 6, login);
  const everyone = [...students, ...seeded];

  const race = args['skip-race'] ? null : await seatRace(admin, students);
  if (race) console.log('Seat race:', JSON.stringify(race));

  const asStudent = (path) => ({
    url: BASE + path,
    requests: [{ method: 'GET', setupRequest: (req) => ({ ...req, headers: { ...req.headers, Authorization: `Bearer ${everyone[Math.floor(Math.random() * everyone.length)]}` } }) }],
  });
  const asAdmin = (path) => ({ url: BASE + path, headers: { Authorization: `Bearer ${admin}` } });

  const cases = [
    // Login is BCrypt-bound (work factor 11) by design: fewer connections.
    ['POST /api/auth/login', { url: BASE + '/api/auth/login', method: 'POST', connections: Math.min(CONNECTIONS, 10),
      headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ universityId: 3001, password: PASSWORD }) }],
    ['GET /api/catalog/courses', asStudent('/api/catalog/courses')],
    ['GET /api/cart', asStudent('/api/cart')],
    ['GET /api/enrollment/my', asStudent('/api/enrollment/my')],
    ['GET /api/schedule/my', asStudent('/api/schedule/my')],
    ['GET /api/grades/my', asStudent('/api/grades/my')],
    ['GET /api/attendance/my', asStudent('/api/attendance/my')],
    ['GET /api/finance/my', asStudent('/api/finance/my')],
    ['GET /api/announcements', asStudent('/api/announcements')],
    ['GET /api/notifications', asStudent('/api/notifications')],
    ['GET /api/notifications/unread-count', asStudent('/api/notifications/unread-count')],
    ['GET /api/admin/users?page=2', asAdmin('/api/admin/users?page=2&pageSize=20')],
    ['GET /api/admin/finance/charges', asAdmin('/api/admin/finance/charges?pageSize=20')],
    ['GET /api/admin/support', asAdmin('/api/admin/support?pageSize=20')],
  ];

  const rows = [];
  for (const [name, opts] of cases) {
    if (ONLY.length && !ONLY.includes(name)) continue;
    await run(name, { ...opts, duration: 2 }); // warm-up
    const r = await run(name, opts);
    rows.push(r);
    console.log(`${name.padEnd(40)} n=${String(r.requests).padStart(6)} ${r.rps.toFixed(0).padStart(5)} req/s  p50 ${r.p50} p90 ${r.p90} p99 ${r.p99} ms  non2xx ${r.non2xx}`);
  }

  console.log('\n| Endpoint | Conn. | Requests | Req/s | p50 ms | p90 ms | p99 ms | Errors |');
  console.log('|---|---:|---:|---:|---:|---:|---:|---:|');
  for (const r of rows)
    console.log(`| \`${r.endpoint}\` | ${r.connections} | ${r.requests} | ${r.rps.toFixed(0)} | ${r.p50} | ${r.p90} | ${r.p99} | ${r.errorPct.toFixed(1)}% |`);

  mkdirSync(dirname(OUT), { recursive: true });
  writeFileSync(OUT, JSON.stringify({
    when: new Date().toISOString(), base: BASE, users: USERS, connections: CONNECTIONS, durationSec: DURATION,
    machine: { cpus: os.cpus().length, cpu: os.cpus()[0]?.model, memGb: Math.round(os.totalmem() / 2 ** 30), platform: `${os.type()} ${os.release()}` },
    race, endpoints: rows,
  }, null, 2));
  if (race && (race.succeeded !== race.capacity || race.seatsTaken !== race.capacity)) {
    console.error('SEAT RACE FAILED: capacity exceeded or not filled');
    process.exitCode = 1;
  }
}

main().catch((e) => { console.error(e); process.exitCode = 1; });
