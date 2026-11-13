import path from 'node:path'
import { fileURLToPath } from 'node:url'
import withBundleAnalyzer from '@next/bundle-analyzer'

const currentDirectory = path.dirname(fileURLToPath(import.meta.url))

const bundleAnalyzer = withBundleAnalyzer({
  enabled: process.env.ANALYZE === 'true',
})

const isVercel = process.env.VERCEL === '1'
const apiBaseUrl = (process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1').replace(/\/+$/, '')
if (isVercel) {
  const endpoint = process.env.INTERNAL_API_BASE_URL && new URL(apiBaseUrl)
  if (
    !endpoint ||
    endpoint.protocol !== 'https:' ||
    endpoint.username ||
    endpoint.password ||
    endpoint.search ||
    endpoint.hash
  )
    throw new Error(
      'Vercel requires an absolute HTTPS INTERNAL_API_BASE_URL without credentials, query or fragment',
    )
}

const contentSecurityPolicy = [
  "default-src 'self'",
  "base-uri 'self'",
  "frame-ancestors 'none'",
  "form-action 'self'",
  "object-src 'none'",
  `script-src 'self' 'unsafe-inline'${process.env.NODE_ENV === 'development' ? " 'unsafe-eval'" : ''}`,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob: https://images.unsplash.com",
  "font-src 'self'",
  `connect-src 'self'${process.env.NODE_ENV === 'development' ? ' ws: wss:' : ''}`,
].join('; ')

const securityHeaders = [
  { key: 'Content-Security-Policy', value: contentSecurityPolicy },
  { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
  { key: 'X-Content-Type-Options', value: 'nosniff' },
  { key: 'X-Frame-Options', value: 'DENY' },
  { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
  { key: 'Cross-Origin-Opener-Policy', value: 'same-origin' },
  { key: 'X-DNS-Prefetch-Control', value: 'off' },
]

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  poweredByHeader: false,
  // Keep SEO metadata in <head> so browsers and Lighthouse can discover it.
  htmlLimitedBots: /.*/,
  experimental: {
    optimizePackageImports: ['lucide-react'],
  },
  async headers() {
    return [
      {
        source: '/:path*',
        headers: securityHeaders,
      },
      {
        source: '/dashboard/:path*',
        headers: [{ key: 'X-Robots-Tag', value: 'noindex, nofollow' }],
      },
      {
        source: '/profile',
        headers: [{ key: 'X-Robots-Tag', value: 'noindex, nofollow' }],
      },
      {
        source: '/login',
        headers: [{ key: 'X-Robots-Tag', value: 'noindex, nofollow' }],
      },
      {
        source: '/register',
        headers: [{ key: 'X-Robots-Tag', value: 'noindex, nofollow' }],
      },
      {
        source: '/auth/:path*',
        headers: [{ key: 'X-Robots-Tag', value: 'noindex, nofollow' }],
      },
    ]
  },
  async rewrites() {
    return {
      beforeFiles: [
        {
          source: '/categories/:slug',
          has: [{ type: 'query', key: 'page', value: '(?<page>[1-9][0-9]*)' }],
          destination: '/render-public/category/:slug/:page',
        },
        { source: '/', destination: '/render-public/home/index' },
        { source: '/categories', destination: '/render-public/categories/index' },
        ...(process.env.NODE_ENV === 'development' || isVercel
          ? [
              {
                source: '/api/v1/:path*',
                destination: `${apiBaseUrl}/:path*`,
              },
            ]
          : []),
      ],
    }
  },
  webpack(config) {
    return {
      ...config,
      resolve: {
        ...config.resolve,
        modules: [path.join(currentDirectory, 'node_modules'), ...config.resolve.modules],
        alias: {
          ...config.resolve.alias,
          'react-router': path.join(currentDirectory, 'src/lib/router-compat.tsx'),
        },
      },
    }
  },
}

export default bundleAnalyzer(nextConfig)
