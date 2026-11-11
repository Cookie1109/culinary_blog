const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? '/api/v1'

if (!apiBaseUrl.startsWith('/') && !URL.canParse(apiBaseUrl)) {
  throw new Error('NEXT_PUBLIC_API_BASE_URL must be an absolute URL or root-relative path')
}

export const publicEnvironment = { NEXT_PUBLIC_API_BASE_URL: apiBaseUrl }
