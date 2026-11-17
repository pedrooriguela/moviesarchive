// Comunicação com a API (.NET) de autenticação.
// A URL base pode ser sobrescrita com VITE_API_URL no arquivo .env
import { clearToken, isTokenExpiring, loadToken, saveToken } from './token.js'

const API_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5197').replace(/\/$/, '')

export class ApiError extends Error {
  constructor(message, { status, field, fieldErrors, code } = {}) {
    super(message)
    this.status = status
    this.code = code // regra que barrou o pedido, quando a API informa (ex.: "plan-full", "subscription-required")
    this.field = field // campo específico (ex.: "username", "email") vindo de 409 Conflict
    this.fieldErrors = fieldErrors ?? {} // erros de validação do [ApiController] (400)
  }
}

// Mapeia os nomes das propriedades dos DTOs C# para os campos do formulário
const FIELD_MAP = {
  name: 'name',
  username: 'name',
  email: 'email',
  password: 'password',
  newpassword: 'password', // senha nova da redefinição
  birthdate: 'birthDate',
}

async function parseError(response) {
  let data = null
  try {
    data = await response.json()
  } catch {
    // corpo vazio ou não-JSON
  }

  // 400 — ValidationProblemDetails: { title, errors: { Name: ["..."], ... } }
  if (response.status === 400 && data?.errors) {
    const fieldErrors = {}
    for (const [key, messages] of Object.entries(data.errors)) {
      const field = FIELD_MAP[key.toLowerCase()] ?? key
      fieldErrors[field] = Array.isArray(messages) ? messages[0] : String(messages)
    }
    return new ApiError('Verifique os campos destacados.', { status: 400, fieldErrors })
  }

  // 409 — { field, message } (usuário/email em uso)
  if (response.status === 409 && data?.message) {
    const field = FIELD_MAP[data.field?.toLowerCase()] ?? data.field
    return new ApiError(data.message, {
      status: 409,
      code: data.code,
      field,
      fieldErrors: field ? { [field]: data.message } : {},
    })
  }

  // 401 — { message }
  if (data?.message) return new ApiError(data.message, { status: response.status, code: data.code })
  if (typeof data === 'string') return new ApiError(data, { status: response.status })

  return new ApiError(`Erro inesperado (HTTP ${response.status}).`, { status: response.status })
}

async function request(path, options = {}) {
  let response
  try {
    response = await fetch(`${API_URL}${path}`, {
      ...options,
      headers: { Accept: 'application/json', ...options.headers },
    })
  } catch {
    throw new ApiError(
      `Não foi possível conectar à API em ${API_URL}. Verifique se o back-end está rodando.`,
    )
  }

  if (!response.ok) throw await parseError(response)

  const text = await response.text()
  return text ? JSON.parse(text) : null
}

/** POST /api/users/signup — body: CreateUserRequestDto { name, email, password } */
export function signUp({ name, email, password, birthDate }) {
  return request('/api/users/signup', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, email, password, birthDate }),
  })
}

/** GET /api/users/signin?Name=...&Password=... — retorna { token } e grava o cookie do refresh token */
export async function signIn({ name, password }) {
  const params = new URLSearchParams({ Name: name, Password: password })
  // credentials: 'include' para o navegador aceitar o cookie httpOnly que a API devolve
  const data = await request(`/api/users/signin?${params}`, { method: 'GET', credentials: 'include' })
  return data.token
}

// ---- Sessão (refresh token) ----
// O JWT dura 15 minutos. Antes de vencer, ou quando a API responde 401, o authFetch pede
// um JWT novo em /api/auth/refresh; o refresh token vai sozinho no cookie httpOnly.

const sessionListeners = new Set()

/** Avisa quando a sessão muda: ('refreshed', token) ou ('expired'). Devolve a função de cancelar. */
export function onSessionChange(listener) {
  sessionListeners.add(listener)
  return () => sessionListeners.delete(listener)
}

function notifySession(event, token) {
  for (const listener of sessionListeners) listener(event, token)
}

let refreshInFlight = null

/** POST /api/auth/refresh — troca o refresh token (cookie) por um JWT novo. Uma renovação por vez. */
export function refreshSession() {
  if (!refreshInFlight) {
    refreshInFlight = request('/api/auth/refresh', { method: 'POST', credentials: 'include' })
      .then((data) => {
        saveToken(data.token)
        notifySession('refreshed', data.token)
        return data.token
      })
      .finally(() => {
        refreshInFlight = null
      })
  }
  return refreshInFlight
}

/** POST /api/auth/logout — encerra a sessão no servidor e apaga o cookie. Falhas são ignoradas. */
export async function logout() {
  try {
    await request('/api/auth/logout', { method: 'POST', credentials: 'include' })
  } catch {
    // Sem conexão: o cookie vence sozinho; o token local é apagado de qualquer forma
  }
}

// Renova; se não der, confere se outra aba já renovou antes de encerrar a sessão
async function renewOrExpire(usedToken) {
  try {
    return await refreshSession()
  } catch (error) {
    const stored = loadToken()
    if (stored && stored !== usedToken && !isTokenExpiring(stored)) return stored

    // Sem conexão com a API não é sessão vencida: mantém o login e mostra o erro
    if (!error.status) throw error

    clearToken()
    notifySession('expired')
    throw new ApiError('Sua sessão expirou. Entre de novo.', { status: 401, code: 'session-expired' })
  }
}

// ---- Recuperação de senha ----
// Se as rotas ou os nomes dos campos no back-end forem outros, ajuste só aqui.

/** Pede o e-mail com o link de redefinição. A API responde 200 mesmo se o e-mail não tiver conta. */
export function requestPasswordReset(email) {
  return request('/api/users/forgot-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(email),
  })
}

/** Troca a senha usando o token que veio no link do e-mail. */
export function resetPassword(token, newPassword) {
  return request('/api/users/reset-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token, newPassword }),
  })
}

function withAuth(options, token) {
  return { ...options, headers: { ...options.headers, Authorization: `Bearer ${token}` } }
}

/** Chamada a endpoint protegido ([Authorize]); renova o JWT sozinho quando ele vence */
export async function authFetch(path, token, options = {}) {
  // O token mais novo pode ter sido salvo por outra chamada (ou aba) depois que a tela o recebeu
  let current = loadToken() || token
  if (isTokenExpiring(current)) current = await renewOrExpire(current)

  try {
    return await request(path, withAuth(options, current))
  } catch (error) {
    if (error.status !== 401) throw error
    // JWT recusado (venceu no caminho, ou a API reiniciou com outra chave): renova e tenta uma vez
    const fresh = await renewOrExpire(current)
    return request(path, withAuth(options, fresh))
  }
}

export function getMyProfile(token) {
  return authFetch('/api/users/me', token)
}

export function setProfilePrivacy(token, isPrivate) {
  return authFetch('/api/users/me/privacy', token, {
    method: 'PUT', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isPrivate }),
  })
}

export function getPhotoSrc(photoUrl) {
  return photoUrl ? `${API_URL}${photoUrl}` : null
}

// birthDate é opcional: a API só aceita se a conta ainda não tiver data de nascimento
export function updateMyProfile(token, { name, birthDate }) {
  return authFetch('/api/users/me', token, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, birthDate }),
  })
}

export function uploadMyProfilePhoto(token, photo) {
  const formData = new FormData()
  formData.append('photo', photo)
  return authFetch('/api/users/me/photo', token, {
    method: 'PUT',
    body: formData,
  })
}

export function removeMyProfilePhoto(token) {
  return authFetch('/api/users/me/photo', token, { method: 'DELETE' })
}

/** DELETE /api/users/me — destination: 'anonymize' (mantém o conteúdo anônimo) ou 'delete' (apaga tudo) */
export function deleteMyAccount(token, { password, destination }) {
  return authFetch('/api/users/me', token, {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ password, destination }),
  })
}

/** GET /api/movies?search=... — busca filmes pelo título */
export function searchMovies(token, search) {
  const params = new URLSearchParams({ search })
  return authFetch(`/api/movies?${params}`, token)
}

/** GET /api/lists/me — todas as listas do usuário logado (inclusive privadas) */
export function getMyLists(token) {
  return authFetch('/api/lists/me', token)
}

/** POST /api/lists — body: CreateListRequestDto { title, description, isPublic } */
export function createList(token, { title, description, isPublic }) {
  return authFetch('/api/lists', token, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title, description: description || null, isPublic }),
  })
}

/** PUT /api/lists/{id} — body: UpdateListRequestDto { title, description, isPublic } */
export function updateList(token, listId, { title, description, isPublic }) {
  return authFetch(`/api/lists/${listId}`, token, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title, description: description || null, isPublic }),
  })
}

export function deleteList(token, listId) {
  return authFetch(`/api/lists/${listId}`, token, { method: 'DELETE' })
}

/** POST /api/lists/{id}/items — adiciona o filme no fim da lista */
export function addListItem(token, listId, movieId) {
  return authFetch(`/api/lists/${listId}/items`, token, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ movieId }),
  })
}

export function removeListItem(token, listId, movieId) {
  return authFetch(`/api/lists/${listId}/items/${movieId}`, token, { method: 'DELETE' })
}

/** PUT /api/lists/{id}/items/order — movieIds com todos os filmes da lista, na nova ordem */
export function updateListItemsOrder(token, listId, movieIds) {
  return authFetch(`/api/lists/${listId}/items/order`, token, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ movieIds }),
  })
}
