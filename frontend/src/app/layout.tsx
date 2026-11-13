import type { Metadata } from 'next'
import { Inter, Playfair_Display as PlayfairDisplay } from 'next/font/google'
import type { ReactNode } from 'react'
import { Providers } from '@/components/providers'
import { SITE_DESCRIPTION, SITE_NAME, SITE_URL } from '@/lib/seo'
import './globals.css'

const inter = Inter({
  subsets: ['latin', 'vietnamese'],
  weight: 'variable',
  style: ['normal', 'italic'],
  display: 'swap',
  variable: '--font-inter',
})

const playfairDisplay = PlayfairDisplay({
  subsets: ['latin', 'vietnamese'],
  weight: 'variable',
  style: ['normal', 'italic'],
  display: 'swap',
  variable: '--font-playfair-display',
})

export const metadata: Metadata = {
  metadataBase: SITE_URL,
  title: {
    default: SITE_NAME,
    template: `%s | ${SITE_NAME}`,
  },
  description: SITE_DESCRIPTION,
  applicationName: SITE_NAME,
  alternates: { canonical: '/' },
  openGraph: {
    type: 'website',
    locale: 'vi_VN',
    siteName: SITE_NAME,
    title: SITE_NAME,
    description: SITE_DESCRIPTION,
    url: '/',
  },
  twitter: {
    card: 'summary_large_image',
    title: SITE_NAME,
    description: SITE_DESCRIPTION,
  },
  robots: { index: true, follow: true },
}

export default async function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  const WebVitals =
    process.env.WEB_VITALS_ENABLED === 'true' ? (await import('@/components/web-vitals')).WebVitals : null
  return (
    <html lang="vi" className={`${inter.variable} ${playfairDisplay.variable}`}>
      <body>
        {WebVitals && <WebVitals />}
        <Providers>{children}</Providers>
      </body>
    </html>
  )
}
