const webBaseUrl = 'http://127.0.0.1:3000'
const apiBaseUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://api:8080/api/v1'

const wait = (milliseconds) => new Promise((resolve) => setTimeout(resolve, milliseconds))

async function waitForDevServer() {
  for (let attempt = 0; attempt < 120; attempt += 1) {
    try {
      const response = await fetch(webBaseUrl)
      if (response.ok) return
    } catch {
      // The Next.js development server is still starting.
    }
    await wait(500)
  }

  throw new Error('Next.js development server did not become ready in time.')
}

async function warmRoute(path) {
  const response = await fetch(`${webBaseUrl}${path}`)
  if (!response.ok) throw new Error(`Unable to warm ${path}: HTTP ${response.status}`)
  await response.arrayBuffer()
  console.log(`[dev-warmup] ${path}`)
}

async function main() {
  await waitForDevServer()
  await warmRoute('/recipes')
  await warmRoute('/categories')

  try {
    const response = await fetch(`${apiBaseUrl}/recipes?page=1&pageSize=1`)
    if (!response.ok) return
    const envelope = await response.json()
    const slug = envelope.data?.[0]?.slug
    if (slug) await warmRoute(`/recipes/${encodeURIComponent(slug)}`)
  } catch (error) {
    console.warn('[dev-warmup] Recipe detail warmup skipped.', error)
  }
}

main().catch((error) => console.warn('[dev-warmup] Warmup skipped.', error))
