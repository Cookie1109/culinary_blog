import http from 'k6/http'
import { check, fail, sleep } from 'k6'
import { Rate, Trend } from 'k6/metrics'

const base = (__ENV.BASE_URL || '').replace(/\/$/, '')
const runId = __ENV.BENCHMARK_RUN_ID || ''
if (!/^http:\/\/(127\.0\.0\.1|localhost):\d+$/.test(base) || !/^g06-[a-f0-9]{12}$/.test(runId)) {
  throw new Error('This preparation runner requires an isolated loopback BASE_URL and BENCHMARK_RUN_ID=g06-<12hex>. Public targets are refused.')
}
const profile = __ENV.PROFILE || 'smoke'
const profiles = {
  smoke: { executor: 'constant-vus', vus: 5, duration: __ENV.SMOKE_DURATION || '30s' },
  load: { executor: 'constant-vus', vus: 100, duration: '10m' },
  stress: { executor: 'ramping-vus', startVUs: 0, stages: [
    { duration: '30s', target: 50 }, { duration: '2m', target: 100 },
    { duration: '2m', target: 150 }, { duration: '30s', target: 0 },
  ] },
}
if (!profiles[profile]) throw new Error('Unknown PROFILE.')
const failures = new Rate('journey_failures')
const duration = new Trend('journey_api_duration', true)
const cachedGet = new Trend('journey_cached_get_duration', true)
export const options = {
  scenarios: { [profile]: profiles[profile] },
  summaryTrendStats: ['avg', 'med', 'p(50)', 'p(95)', 'p(99)', 'max'],
  thresholds: {
    journey_failures: ['rate==0'],
    journey_api_duration: ['p(95)<=500', 'p(99)<=1000'],
    journey_cached_get_duration: ['p(50)<=150'],
  },
}

function json(response) {
  try { return response.json() } catch { fail('Expected JSON response; no response body is logged.') }
}
function call(method, path, token, body, expected, version) {
  const headers = { 'Content-Type': 'application/json' }
  if (token) headers.Authorization = `Bearer ${token}`
  if (version !== undefined) headers['If-Match'] = `"${version}"`
  const response = http.request(method, base + path, body === undefined ? null : JSON.stringify(body), {
    headers, tags: { name: method + ' ' + path.replace(/[a-f0-9-]{36}/g, '[id]') },
  })
  duration.add(response.timings.duration)
  const ok = check(response, { 'expected business status': (r) => r.status === expected })
  failures.add(!ok)
  if (!ok) fail(`Journey ${method} failed with HTTP ${response.status}; body and credentials omitted.`)
  if (method === 'GET' && response.headers.Age !== undefined) cachedGet.add(response.timings.duration)
  return response
}

export function setup() {
  if (http.get(base + '/health/ready').status !== 200) fail('Isolated target not ready.')
  const category = json(http.get(base + '/api/v1/categories')).data[0].id
  const count = profile === 'smoke' ? 5 : profile === 'load' ? 100 : 150
  for (let index = 0; index < count; index++) {
    const registration = http.post(base + '/api/v1/auth/register', JSON.stringify({
      email: `${runId}-${index}@g06.test`, password: 'Synthetic#G06Journey123', displayName: 'G06 fixture',
    }), { headers: { 'Content-Type': 'application/json' }, tags: { name: 'control register' } })
    if (registration.status !== 201) fail(`Synthetic registration failed with HTTP ${registration.status}.`)
  }
  for (const path of ['/api/v1/categories', '/api/v1/recipes?pageSize=12']) {
    if (http.get(base + path).status !== 200) fail('Warm-up failed.')
    if (http.get(base + path).status !== 200) fail('Warm-up failed.')
  }
  return { category }
}

// VU-local memory is not serialized into setup_data by legacy summary exports.
let token
export default function ({ category }) {
  if (!token) {
    const login = http.post(base + '/api/v1/auth/login', JSON.stringify({
      email: `${runId}-${__VU - 1}@g06.test`, password: 'Synthetic#G06Journey123',
    }), { headers: { 'Content-Type': 'application/json' }, tags: { name: 'control login' } })
    if (login.status !== 200) fail(`Synthetic login failed with HTTP ${login.status}.`)
    token = json(login).data.accessToken
  }
  call('GET', '/api/v1/categories', undefined, undefined, 200)
  call('GET', '/api/v1/recipes?pageSize=12', undefined, undefined, 200)
  const title = `G06 bench ${runId} ${__VU} ${__ITER}`
  const created = json(call('POST', '/api/v1/recipes', token, {
    title, description: 'Synthetic benchmark only.', categoryId: category,
    prepTime: 10, cookTime: 20, servings: 2, difficulty: 'easy', instructions: 'Cook.',
  }, 201)).data
  const changed = json(call('PUT', `/api/v1/recipes/${created.id}`, token, { title: title + ' updated' }, 200, created.version)).data
  const owned = json(call('GET', `/api/v1/me/recipes/${created.id}`, token, undefined, 200)).data
  const correct = check(owned, { 'owned write is readable': (r) => r.id === created.id && r.title === changed.title })
  failures.add(!correct)
  call('DELETE', `/api/v1/recipes/${created.id}`, token, undefined, 204, changed.version)
  sleep(1)
}

export function handleSummary(data) {
  return {
    [__ENV.SUMMARY_PATH || `journey-${runId}.json`]: JSON.stringify({ runId, profile, metrics: data.metrics }, null, 2),
    stdout: 'G06 journey metrics written without setup/authentication data.\n',
  }
}
