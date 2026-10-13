// Armazenamento e leitura do JWT emitido pela API.
const STORAGE_KEY = 'booksarchive.token'

export function saveToken(token) {
  localStorage.setItem(STORAGE_KEY, token)
}

export function loadToken() {
  return localStorage.getItem(STORAGE_KEY)
}

export function clearToken() {
  localStorage.removeItem(STORAGE_KEY)
}

// Decodifica o payload (sem validar a assinatura — isso é papel da API).
export function decodeToken(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=')
    const bytes = Uint8Array.from(atob(padded), (c) => c.charCodeAt(0))
    return JSON.parse(new TextDecoder().decode(bytes))
  } catch {
    return null
  }
}

// Claims geradas pelo JwtService: sub (Id), email, name, exp
export function getUserFromToken(token) {
  const payload = token ? decodeToken(token) : null
  if (!payload) return null
  if (payload.exp && payload.exp * 1000 <= Date.now()) return null // expirado

  return {
    id: payload.sub,
    name: payload.name,
    email: payload.email,
    expiresAt: payload.exp ? new Date(payload.exp * 1000) : null,
  }
}
