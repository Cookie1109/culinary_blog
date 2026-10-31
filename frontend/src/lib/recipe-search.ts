export function normalizeRecipeSearch(value: string) {
  return value.normalize('NFKC').trim().replace(/\s+/gu, ' ')
}

export function recipeSearchValidation(query: string) {
  const value = normalizeRecipeSearch(query)
  if (value.length === 1) return 'Từ khóa cần ít nhất 2 ký tự.'
  if (value.length > 100) return 'Từ khóa tối đa 100 ký tự.'
  if (value && !/[\p{L}\p{N}]/u.test(value)) return 'Nhập tên món bằng chữ hoặc số.'
  return ''
}

export function foldVietnamese(value: string) {
  return value.normalize('NFD').replace(/\p{M}/gu, '').replace(/[đĐ]/gu, 'd').toLowerCase()
}

function words(value: string) {
  return value.match(/[\p{L}\p{N}\p{M}]+/gu) ?? []
}

function recipeSearchParts(text: string, query: string) {
  const terms = words(normalizeRecipeSearch(query)).map(foldVietnamese)
  const known = new Set(words(text).map(foldVietnamese))
  return text.split(/([\p{L}\p{N}\p{M}]+)/gu).map((value) => {
    const folded = foldVietnamese(value)
    const matched = terms.some((term) =>
      term.length <= 2 && known.has(term) ? folded === term : folded.startsWith(term),
    )
    return { value, matched }
  })
}

export function recipeSearchSnippet(text: string, query: string) {
  if (!query || text.length <= 160) return text
  const parts = recipeSearchParts(text, query)
  const match = parts.findIndex((part) => part.matched)
  const offset = parts.slice(0, Math.max(match, 0)).reduce((length, part) => length + part.value.length, 0)
  const start = Math.max(0, offset - 45)
  const boundary = start ? text.indexOf(' ', start) + 1 : 0
  const end = Math.min(text.length, boundary + 160)
  return `${boundary ? '… ' : ''}${text.slice(boundary, end)}${end < text.length ? '…' : ''}`
}

export function recipeSearchPages(page: number, total: number) {
  return [...new Set([1, page - 1, page, page + 1, total])]
    .filter((value) => value >= 1 && value <= total)
    .sort((a, b) => a - b)
}
