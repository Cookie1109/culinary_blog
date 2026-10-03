const mediaPath = /^\/api\/v1\/media\/([^/]+)\/([^/?#]+)/

function proxiedMediaUrl(source: string, scope: 'public-media' | 'private-media') {
  const match = source.match(mediaPath)
  return match ? `/api/${scope}/${match[1]}/${match[2]}` : source
}

export function publicMediaUrl(source: string) {
  return proxiedMediaUrl(source, 'public-media')
}

export function privateMediaUrl(source: string) {
  return proxiedMediaUrl(source, 'private-media')
}
