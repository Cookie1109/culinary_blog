import { ImageResponse } from 'next/og'

export const alt = 'Culinary Blog — Công thức nấu ăn từ cộng đồng'
export const size = { width: 1200, height: 630 }
export const contentType = 'image/png'

export default function OpenGraphImage() {
  return new ImageResponse(
    <div
      style={{
        alignItems: 'center',
        background: '#f7f1e8',
        color: '#2f2a25',
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        justifyContent: 'center',
        padding: '72px',
        textAlign: 'center',
        width: '100%',
      }}
    >
      <div style={{ color: '#a3462d', fontSize: 30, letterSpacing: 8, textTransform: 'uppercase' }}>
        Khám phá · Nấu ăn · Chia sẻ
      </div>
      <div style={{ fontSize: 94, fontWeight: 700, marginTop: 32 }}>Culinary Blog</div>
      <div style={{ fontSize: 38, marginTop: 24 }}>Công thức ngon từ căn bếp cộng đồng</div>
    </div>,
    size,
  )
}
