// Local synthetic profile fixture from docs/specs/phase-0/openapi.v1.yaml.
// This verifies the frontend, not backend token validation or Google OAuth.
const http = require('http')
const initialUser = {
  id: '33333333-3333-4333-8333-333333333333', displayName: 'Profile Browser Author',
  email: 'profile@example.test', avatarUrl: null, bio: 'Initial biography', roles: ['Author'],
  emailConfirmed: true, isActive: true, createdAt: '2026-01-01T00:00:00Z',
}
http.createServer(async (request, response) => {
  const url = new URL(request.url, 'http://localhost')
  const send = (status, body) => {
    response.writeHead(status, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' })
    response.end(body === undefined ? undefined : JSON.stringify(body))
  }
  const user = (await pm.state.get('culinary.profile.user')) ?? initialUser
  if (request.method === 'POST' && url.pathname === '/api/v1/auth/login') {
    if (request.body?.email !== initialUser.email || request.body?.password !== 'Synthetic#Password1')
      return send(401, { title: 'Authentication failed', status: 401, code: 'AUTH_INVALID_CREDENTIALS' })
    return send(200, { data: { user, accessToken: 'synthetic-access-token', refreshToken: 'synthetic-refresh-token', expiresAt: new Date(Date.now() + 900000).toISOString() } })
  }
  if (request.method === 'GET' && url.pathname === '/api/v1/auth/me') return send(200, { data: user })
  if (request.method === 'PATCH' && url.pathname === '/api/v1/auth/me') {
    const body = request.body ?? {}
    if ((body.displayName !== undefined && (typeof body.displayName !== 'string' || body.displayName.trim().length < 2 || body.displayName.length > 100)) ||
        (body.bio !== undefined && body.bio !== null && (typeof body.bio !== 'string' || body.bio.length > 2000)) ||
        (body.avatarUrl !== undefined && body.avatarUrl !== null && !/^https?:\/\//.test(body.avatarUrl)))
      return send(400, { title: 'Validation failed', status: 400, code: 'VALIDATION_ERROR' })
    const next = { ...user }
    for (const field of ['displayName', 'bio', 'avatarUrl']) if (Object.hasOwn(body, field)) next[field] = body[field]
    await pm.state.set('culinary.profile.user', next)
    return send(200, { data: next })
  }
  if (request.method === 'POST' && url.pathname === '/api/v1/auth/logout') return send(204)
  if (request.method === 'GET' && url.pathname === '/api/v1/categories') return send(200, { data: [] })
  if (request.method === 'GET' && url.pathname === '/api/v1/recipes') return send(200, { data: [], meta: { page: 1, pageSize: 12, total: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false } })
  if (request.method === 'GET' && url.pathname === '/api/v1/operations/maintenance') return send(200, { data: { enabled: false, message: null, announcedAtUtc: null, startsAtUtc: null, endsAtUtc: null }, meta: {} })
  return send(501, { title: 'Outside the profile fixture contract', status: 501 })
}).listen(Number(process.env.PORT), '127.0.0.1')
