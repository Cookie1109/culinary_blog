import http from 'k6/http'
import { check, fail, sleep } from 'k6'
import { Gauge, Rate, Trend } from 'k6/metrics'

const profile = __ENV.PROFILE || 'smoke'
const baseUrl = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/$/, '')
const prometheusUrl = (__ENV.PROMETHEUS_URL || 'http://127.0.0.1:9090').replace(/\/$/, '')
const requireCacheMetrics = (__ENV.REQUIRE_CACHE_METRICS || 'true').toLowerCase() === 'true'
const cacheMetricsSettleSeconds = Number(__ENV.CACHE_METRICS_SETTLE_SECONDS || 20)
const runId = `${Date.now()}`

const apiFailures = new Rate('api_failures')
const apiWarmDuration = new Trend('api_warm_duration', true)
const apiColdDuration = new Trend('api_cold_duration', true)
const cacheHitRate = new Gauge('cache_hit_rate')

const profiles = {
  smoke: {
    executor: 'constant-vus',
    vus: 5,
    duration: '30s',
    gracefulStop: '10s',
  },
  load: {
    executor: 'constant-vus',
    vus: 100,
    duration: '5m',
    gracefulStop: '30s',
  },
  stress: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '30s', target: 20 },
      { duration: '30s', target: 50 },
      { duration: '30s', target: 100 },
      { duration: '3m', target: 100 },
      { duration: '30s', target: 0 },
    ],
    gracefulRampDown: '30s',
  },
}

if (!profiles[profile]) {
  throw new Error(`Unsupported PROFILE '${profile}'. Use smoke, load, or stress.`)
}

export const options = {
  scenarios: { [profile]: profiles[profile] },
  summaryTrendStats: ['avg', 'min', 'med', 'p(50)', 'p(95)', 'p(99)', 'max'],
  thresholds: {
    api_failures: ['rate<0.01'],
    api_warm_duration: ['p(50)<150', 'p(95)<500', 'p(99)<1000'],
    api_cold_duration: ['p(95)<500', 'p(99)<1000'],
    cache_hit_rate: ['value>=0.80'],
  },
}

function controlGet(path) {
  return http.get(`${baseUrl}${path}`, {
    headers: { Accept: 'application/json' },
    tags: { target: 'control', name: path },
  })
}

function parseJson(response, description) {
  try {
    return response.json()
  } catch (error) {
    fail(`${description} did not return JSON: ${error}`)
  }
}

function queryPrometheus(metricName) {
  const query = encodeURIComponent(`sum(${metricName})`)
  const response = http.get(`${prometheusUrl}/api/v1/query?query=${query}`, {
    tags: { target: 'control', name: 'prometheus-query' },
  })

  if (response.status !== 200) return null

  const payload = parseJson(response, 'Prometheus')
  const value = payload?.data?.result?.[0]?.value?.[1]
  return value === undefined ? null : Number(value)
}

function readCacheCounters() {
  return {
    hits: queryPrometheus('cache_hits_total'),
    misses: queryPrometheus('cache_misses_total'),
  }
}

function buildWarmPaths(categories, recipes) {
  const paths = [
    '/api/v1/categories',
    '/api/v1/recipes?page=1&pageSize=12',
    '/api/v1/recipes?page=2&pageSize=12',
    `/api/v1/recipes/search?q=${encodeURIComponent('phở')}&page=1&pageSize=12&sort=relevance`,
    '/api/v1/recipes/search?page=1&pageSize=12&sort=newest',
  ]

  if (categories[0]?.slug) {
    paths.push(`/api/v1/categories/${encodeURIComponent(categories[0].slug)}?page=1&pageSize=12`)
  }

  for (const recipe of recipes.slice(0, 3)) {
    if (recipe.slug) paths.push(`/api/v1/recipes/${encodeURIComponent(recipe.slug)}`)
  }

  return paths
}

export function setup() {
  const health = controlGet('/health/ready')
  if (health.status !== 200) {
    fail(`Target is not ready: ${baseUrl}/health/ready returned ${health.status}`)
  }

  const categoriesResponse = controlGet('/api/v1/categories')
  const recipesResponse = controlGet('/api/v1/recipes?page=1&pageSize=12')
  if (categoriesResponse.status !== 200 || recipesResponse.status !== 200) {
    fail('Unable to discover the synthetic performance dataset.')
  }

  const categories = parseJson(categoriesResponse, 'Categories response').data || []
  const recipes = parseJson(recipesResponse, 'Recipes response').data || []
  const warmPaths = buildWarmPaths(categories, recipes)

  for (let pass = 0; pass < 2; pass += 1) {
    for (const path of warmPaths) {
      const response = controlGet(path)
      if (response.status !== 200) fail(`Warm-up failed for ${path}: ${response.status}`)
    }
  }

  sleep(cacheMetricsSettleSeconds)
  const cacheBaseline = readCacheCounters()
  if (requireCacheMetrics && (cacheBaseline.hits === null || cacheBaseline.misses === null)) {
    fail(`Cache metrics are unavailable from ${prometheusUrl}.`)
  }

  return { warmPaths, cacheBaseline }
}

function apiGet(path, cacheClass) {
  const response = http.get(`${baseUrl}${path}`, {
    headers: { Accept: 'application/json' },
    tags: {
      target: 'api',
      cache: cacheClass,
      name: cacheClass === 'cold' ? '/api/v1/recipes/search?cold-key' : path,
    },
  })

  const succeeded = check(response, {
    'API returned 200': (result) => result.status === 200,
  })
  apiFailures.add(!succeeded)

  if (cacheClass === 'warm') apiWarmDuration.add(response.timings.duration)
  else apiColdDuration.add(response.timings.duration)
}

export default function (data) {
  const isWarmRequest = (__ITER + __VU) % 10 !== 0
  if (isWarmRequest) {
    apiGet(data.warmPaths[(__ITER + __VU) % data.warmPaths.length], 'warm')
  } else {
    const uniqueQuery = `zz${runId}${__VU}${__ITER}`
    apiGet(`/api/v1/recipes/search?q=${uniqueQuery}&page=1&pageSize=12&sort=relevance`, 'cold')
  }

  sleep(1)
}

export function teardown(data) {
  if (data.cacheBaseline.hits === null || data.cacheBaseline.misses === null) {
    cacheHitRate.add(0)
    return
  }

  sleep(cacheMetricsSettleSeconds)
  const finalCounters = readCacheCounters()
  const hitDelta = Math.max(0, (finalCounters.hits ?? 0) - data.cacheBaseline.hits)
  const missDelta = Math.max(0, (finalCounters.misses ?? 0) - data.cacheBaseline.misses)
  const measuredRequests = hitDelta + missDelta
  const ratio = measuredRequests > 0 ? hitDelta / measuredRequests : 0

  cacheHitRate.add(ratio)
  console.log(
    `Application cache: ${hitDelta} hits, ${missDelta} misses, ${(ratio * 100).toFixed(2)}% hit rate`,
  )
}
