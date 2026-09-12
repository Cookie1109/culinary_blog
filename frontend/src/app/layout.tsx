import type { Metadata } from 'next'
import { Inter, Playfair_Display } from 'next/font/google'
import type { ReactNode } from 'react'
import { Providers } from '@/components/providers'
import './globals.css'

const inter = Inter({
  subsets: ['latin', 'vietnamese'],
  weight: 'variable',
  style: ['normal', 'italic'],
  display: 'swap',
  variable: '--font-inter',
})

const playfairDisplay = Playfair_Display({
  subsets: ['latin', 'vietnamese'],
  weight: 'variable',
  style: ['normal', 'italic'],
  display: 'swap',
  variable: '--font-playfair-display',
})

export const metadata: Metadata = {
  title: {
    default: 'Culinary Blog',
    template: '%s · Culinary Blog',
  },
  description: 'Công thức nấu ăn được tuyển chọn và chia sẻ bởi cộng đồng.',
}

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="vi" className={`${inter.variable} ${playfairDisplay.variable}`}>
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  )
}
