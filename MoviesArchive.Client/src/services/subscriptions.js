import { authFetch } from './api.js'

// Planos, assinatura do usuário logado e convites do plano familiar.
// Não tem modo de exemplo (mock): fala sempre com a API, como as listas e o perfil.
// Toda ação devolve a situação atualizada:
//   { subscription | null, receivedInvites, hasBirthDate, isAdult, canSubscribe }
// Quando uma regra barra o pedido, o erro traz `code` (ex.: "plan-full", "minor") e a mensagem pronta.

const JSON_HEADERS = { 'Content-Type': 'application/json' }

/** GET /api/plans — planos à venda (individual antes do familiar) */
export function getPlans(token) {
  return authFetch('/api/plans', token)
}

/** GET /api/subscriptions/me */
export function getSubscriptionState(token) {
  return authFetch('/api/subscriptions/me', token)
}

/** POST /api/subscriptions — contratar (só maiores de 18, sem assinatura) */
export function subscribe(planId, token) {
  return authFetch('/api/subscriptions', token, {
    method: 'POST',
    headers: JSON_HEADERS,
    body: JSON.stringify({ planId }),
  })
}

/** PUT /api/subscriptions/me/plan — upgrade ou downgrade (só o titular) */
export function changePlan(planId, token) {
  return authFetch('/api/subscriptions/me/plan', token, {
    method: 'PUT',
    headers: JSON_HEADERS,
    body: JSON.stringify({ planId }),
  })
}

/** DELETE /api/subscriptions/me — cancelar (só o titular); todos voltam a ser contas gratuitas */
export function cancelSubscription(token) {
  return authFetch('/api/subscriptions/me', token, { method: 'DELETE' })
}

/** DELETE /api/subscriptions/me/membership — sair do plano familiar de outra pessoa */
export function leaveSubscription(token) {
  return authFetch('/api/subscriptions/me/membership', token, { method: 'DELETE' })
}

/** POST /api/subscriptions/me/invites — convidar pelo nome de usuário */
export function inviteToPlan(userName, token) {
  return authFetch('/api/subscriptions/me/invites', token, {
    method: 'POST',
    headers: JSON_HEADERS,
    body: JSON.stringify({ userName }),
  })
}

/** DELETE /api/subscriptions/me/invites/{id} */
export function cancelInvite(inviteId, token) {
  return authFetch(`/api/subscriptions/me/invites/${inviteId}`, token, { method: 'DELETE' })
}

/** DELETE /api/subscriptions/me/members/{id} */
export function removeMember(memberId, token) {
  return authFetch(`/api/subscriptions/me/members/${memberId}`, token, { method: 'DELETE' })
}

/** POST /api/invites/{id}/accept */
export function acceptInvite(inviteId, token) {
  return authFetch(`/api/invites/${inviteId}/accept`, token, { method: 'POST' })
}

/** POST /api/invites/{id}/decline */
export function declineInvite(inviteId, token) {
  return authFetch(`/api/invites/${inviteId}/decline`, token, { method: 'POST' })
}

const PRICE = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

/** 19.9 -> "R$ 19,90" */
export function formatPrice(value) {
  return PRICE.format(value)
}

/** 1 -> "1 pessoa", 4 -> "4 pessoas" */
export function formatPeople(count) {
  return count === 1 ? '1 pessoa' : `${count} pessoas`
}
