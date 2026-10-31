import { beforeAll, jest, test, expect } from '@jest/globals'

jest.mock('@/auth', () => ({ handlers: { GET: jest.fn(), POST: jest.fn() } }))

let handlers
let GET
let POST
beforeAll(async () => {
  ;({ handlers } = await import('@/auth'))
  ;({ GET, POST } = await import('../src/app/api/auth/[...nextauth]/route'))
})

test('a late session read cannot renew the cookie cleared by logout', async () => {
  const response = { headers: new Map([['set-cookie', 'synthetic-session']]) }
  handlers.GET.mockResolvedValueOnce(response)
  expect(await GET({ nextUrl: { pathname: '/api/auth/session' } })).toBe(response)
  expect(response.headers.has('set-cookie')).toBe(false)
})

test.each(['/api/auth/csrf', '/api/auth/callback/google'])(
  'preserves required cookies from %s',
  async (pathname) => {
    const response = { headers: new Map([['set-cookie', 'synthetic-required-cookie']]) }
    handlers.GET.mockResolvedValueOnce(response)
    await GET({ nextUrl: { pathname } })
    expect(response.headers.has('set-cookie')).toBe(true)
  },
)

test('explicit sign-in, sign-out and session writes retain the Auth.js POST handler', () => {
  expect(POST).toBe(handlers.POST)
})
