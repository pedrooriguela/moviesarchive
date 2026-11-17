// Armazenamento e leitura do JWT emitido pela API.
// O JWT dura pouco (15 min); quem mantém a sessão é o refresh token, num cookie httpOnly
// que o JavaScript não lê (ver services/api.js: refreshSession).
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

// Vence em menos de `marginMs` (ou já venceu)?
export function isTokenExpiring(token, marginMs = 30_000) {
  const payload = token ? decodeToken(token) : null
  if (!payload?.exp) return true
  return payload.exp * 1000 - Date.now() < marginMs
}

// Claims geradas pelo JwtService: sub (Id), email, name, exp.
// Com allowExpired, devolve o usuário mesmo com o JWT vencido: a sessão continua
// enquanto o refresh token valer, e o authFetch renova o JWT quando precisar.
export function getUserFromToken(token, { allowExpired = false } = {}) {
  const payload = token ? decodeToken(token) : null
  if (!payload) return null
  if (!allowExpired && payload.exp && payload.exp * 1000 <= Date.now()) return null // expirado

  return {
    id: payload.sub,
    name: payload.name,
    email: payload.email,
    expiresAt: payload.exp ? new Date(payload.exp * 1000) : null,
  }
}
