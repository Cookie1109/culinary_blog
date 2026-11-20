// Public DTO fixtures follow docs/specs/phase-0/openapi.v1.yaml. The local-only
// control endpoint changes a revision so ISR can be checked without user data.
const http = require('http')

function fixtures(revision) {
  const category = {
    id: '11111111-1111-4111-8111-111111111111', name: `ISR category revision ${revision}`,
    slug: 'isr-category', description: 'Synthetic public category', imageUrl: null,
    orderIndex: 0, recipeCount: 1,
  }
  const recipe = {
    id: '22222222-2222-4222-8222-222222222222', title: `ISR recipe revision ${revision} with a long descriptive recipe name for metadata verification`,
    slug: 'isr-recipe', description: 'Synthetic published recipe for HTTP cache verification.',
    prepTime: 10, cookTime: 20, servings: 4, difficulty: 'easy', status: 'published',
    primaryImageUrl: null, category,
    author: { id: '33333333-3333-4333-8333-333333333333', displayName: 'ISR Test Author', email: 'isr@example.test', avatarUrl: null, bio: null, roles: ['Author'], emailConfirmed: true, isActive: true, createdAt: '2026-01-01T00:00:00Z' },
    createdAt: '2026-01-01T00:00:00Z', publishedAt: '2026-01-02T00:00:00Z',
    version: revision, instructions: '', nutrition: null, images: [], relevanceScore: null,
    ingredients: [{ id: '44444444-4444-4444-8444-444444444444', name: 'Gạo', quantity: 200, unit: 'g', notes: null, orderIndex: 0 }],
    steps: [{ id: '55555555-5555-4555-8555-555555555555', stepNumber: 1, title: 'Nấu', description: 'Nấu chín nguyên liệu.', timerMinutes: 20, imageUrl: null }],
  }
  return { category, recipe }
}

http.createServer(async (request, response) => {
  const url = new URL(request.url, 'http://localhost')
  const send = (status, body) => {
    response.writeHead(status, { 'Content-Type': 'application/json' })
    response.end(JSON.stringify(body))
  }
  if (request.method === 'POST' && url.pathname === '/__fixture/revision') {
    const revision = request.body?.revision
    if (!Number.isInteger(revision) || revision < 1 || revision > 1000) return send(400, { error: 'Invalid fixture revision' })
    await pm.state.set('culinary.public.revision', revision)
    return send(200, { revision })
  }
  const revision = (await pm.state.get('culinary.public.revision')) ?? 1
  const { category, recipe } = fixtures(revision)
  const page = Number(url.searchParams.get('page') || 1)
  const pageSize = Number(url.searchParams.get('pageSize') || 12)
  const meta = { page, pageSize, total: 1, totalPages: 1, hasNextPage: false, hasPreviousPage: page > 1 }
  if (request.method === 'GET') {
    if (url.pathname === '/api/v1/recipes/isr-old-slug') {
      response.writeHead(301, { Location: '/api/v1/recipes/isr-recipe' })
      return response.end()
    }
    if (url.pathname === '/api/v1/categories') return send(200, { data: [category] })
    if (url.pathname === '/api/v1/recipes') return send(200, { data: page === 1 ? [recipe] : [], meta })
    if (url.pathname === '/api/v1/recipes/search') return send(200, { data: page === 1 ? [recipe] : [], meta })
    if (url.pathname === '/api/v1/recipes/isr-recipe') return send(200, { data: recipe })
    if (url.pathname === '/api/v1/categories/isr-category') return send(200, { data: { category, recipes: page === 1 ? [recipe] : [] }, meta })
    if (url.pathname.startsWith('/api/v1/categories/') || url.pathname.startsWith('/api/v1/recipes/')) {
      return send(404, { title: 'Not found', status: 404, code: 'RESOURCE_NOT_FOUND' })
    }
  }
  return send(501, { error: 'Endpoint outside the public ISR fixture contract' })
}).listen(Number(process.env.PORT), '127.0.0.1')
