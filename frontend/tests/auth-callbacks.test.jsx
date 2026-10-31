import { beforeAll, beforeEach, afterAll, jest, test, expect } from '@jest/globals'

jest.mock('next-auth', () => ({
  __esModule: true,
  default: jest.fn(() => ({ handlers: {}, auth: jest.fn() })),
}))
jest.mock('next-auth/providers/credentials', () => ({ __esModule: true, default: (config) => config }))
jest.mock('next-auth/providers/google', () => ({ __esModule: true, default: {} }))

let config
const originalFetch = globalThis.fetch
const user = Object.freeze({ id: 'fixture', displayName: 'Author', roles: ['Author'] })
const backend = Object.freeze({ accessToken: 'synthetic-access', expiresAt: '2026-10-29T10:00:00Z', user })
beforeAll(async () => {
  const { default: configure } = await import('next-auth')
  await import('../src/auth')
  ;[[config]] = configure.mock.calls
})
beforeEach(() => {
  globalThis.fetch = jest.fn()
})
afterAll(() => {
  globalThis.fetch = originalFetch
})

test('credentials verify the backend user instead of trusting caller profile fields', async () => {
  globalThis.fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ data: user }) })
  const result = await config.providers[0].authorize({ ...backend, user: { id: 'forged' } })
  expect(result.id).toBe('fixture')
  expect(result.backendSession.user).toBe(user)
  expect(globalThis.fetch.mock.calls[0][1].headers.Authorization).toBe('Bearer synthetic-access')
  globalThis.fetch.mockResolvedValueOnce({ ok: false })
  expect(await config.providers[0].authorize(backend)).toBeNull()
})

test('JWT credential sign-in and profile sync preserve immutable callback inputs', async () => {
  const token = Object.freeze({ sub: 'fixture' })
  const signedIn = await config.callbacks.jwt({ token, user: { backendSession: backend } })
  expect(signedIn.backend).toBe(backend)
  expect(token).toEqual({ sub: 'fixture' })
  const updatedUser = { ...user, displayName: 'Updated' }
  globalThis.fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ data: updatedUser }) })
  const original = Object.freeze({ ...token, backend })
  const updated = await config.callbacks.jwt({ token: original, trigger: 'update' })
  expect(updated.backend.user).toBe(updatedUser)
  expect(original.backend.user).toBe(user)
})

test.each(['network', 'rejected'])(
  'profile sync %s failure retains the working backend session',
  async (failure) => {
    if (failure === 'network') globalThis.fetch.mockRejectedValueOnce(new Error('Synthetic outage'))
    else globalThis.fetch.mockResolvedValueOnce({ ok: false })
    const token = Object.freeze({ backend })
    const result = await config.callbacks.jwt({ token, trigger: 'update' })
    expect(result.backend).toBe(backend)
  },
)

test('Google exchange revokes the temporary refresh token and exposes only the access session', async () => {
  globalThis.fetch
    .mockResolvedValueOnce({
      ok: true,
      json: async () => ({ data: { ...backend, refreshToken: 'synthetic-refresh' } }),
    })
    .mockResolvedValueOnce({ ok: true })
  const result = await config.callbacks.jwt({
    token: Object.freeze({}),
    account: { provider: 'google', id_token: 'synthetic-google' },
  })
  expect(result.backend).toEqual(backend)
  expect(JSON.stringify(result)).not.toContain('synthetic-refresh')
  expect(globalThis.fetch.mock.calls[1][0]).toMatch(/auth\/logout$/)
  expect(JSON.parse(globalThis.fetch.mock.calls[1][1].body)).toEqual({ refreshToken: 'synthetic-refresh' })
})

test('session projection and private-route authorization require a backend identity', async () => {
  const session = Object.freeze({ expires: 'synthetic' })
  expect(await config.callbacks.session({ session, token: { backend } })).toEqual({
    ...session,
    backendUser: user,
    accessToken: backend.accessToken,
    accessTokenExpiresAt: backend.expiresAt,
  })
  expect(session).toEqual({ expires: 'synthetic' })
  expect(config.callbacks.authorized({ auth: null })).toBe(false)
  expect(config.callbacks.authorized({ auth: { user: {} } })).toBe(false)
  expect(config.callbacks.authorized({ auth: { backendUser: user } })).toBe(true)
})
