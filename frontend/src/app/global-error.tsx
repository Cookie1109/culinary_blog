'use client'

export default function GlobalError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <html lang="vi">
      <body className="grid min-h-screen place-items-center bg-[#faf8f5] px-4 text-center text-[#2c2a28]">
        <main>
          <h1 className="mb-4 font-serif text-4xl">Culinary Blog tạm thời gián đoạn.</h1>
          <button className="bg-[#c55333] px-6 py-3 text-white" onClick={reset}>
            Tải lại trang
          </button>
        </main>
      </body>
    </html>
  )
}
