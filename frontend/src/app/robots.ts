import type { MetadataRoute } from 'next'

export default function robots(): MetadataRoute.Robots {
  const siteUrl = (process.env.SITE_URL ?? 'http://localhost:8080').replace(/\/$/, '')

  return {
    rules: [
      {
        userAgent: '*',
        allow: '/',
        disallow: ['/dashboard/', '/profile', '/login', '/register'],
      },
    ],
    sitemap: `${siteUrl}/sitemap.xml`,
  }
}
