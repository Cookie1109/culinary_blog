import path from 'node:path'
import { fileURLToPath } from 'node:url'

const currentDirectory = path.dirname(fileURLToPath(import.meta.url))

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  webpack(config) {
    config.resolve.modules = [path.join(currentDirectory, 'node_modules'), ...config.resolve.modules]
    config.resolve.alias['react-router'] = path.join(currentDirectory, 'src/lib/router-compat.tsx')
    return config
  },
}

export default nextConfig
