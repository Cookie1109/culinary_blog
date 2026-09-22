import path from 'node:path'
import { fileURLToPath } from 'node:url'

const currentDirectory = path.dirname(fileURLToPath(import.meta.url))

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  async redirects() {
    return [
      { source: '/auth/login', destination: '/login', permanent: false },
      { source: '/auth/register', destination: '/register', permanent: false },
    ]
  },
  async rewrites() {
    return process.env.NODE_ENV === 'development'
      ? [
          {
            source: '/api/v1/:path*',
            destination: `${process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'}/:path*`,
          },
        ]
      : []
  },
  webpack(config) {
    config.resolve.modules = [path.join(currentDirectory, 'node_modules'), ...config.resolve.modules]
    config.resolve.alias['react-router'] = path.join(currentDirectory, 'src/lib/router-compat.tsx')
    return config
  },
}

export default nextConfig
