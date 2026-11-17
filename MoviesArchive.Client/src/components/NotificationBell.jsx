import { useCallback, useEffect, useId, useRef, useState } from 'react'
import Avatar from './Avatar.jsx'
import {
  getMyNotifications,
  getUnreadNotificationCount,
  markAllNotificationsRead,
  isPlanNotification,
  markNotificationRead,
} from '../services/notifications.js'
import { acceptInvite, declineInvite } from '../services/subscriptions.js'

const POLL_MS = 60_000 // de quanto em quanto tempo o contador é atualizado

// Sino em pixel art (grade 9 x 10)
const BELL = [
  '....#....',
  '...###...',
  '..#####..',
  '..#####..',
  '..#####..',
  '..#####..',
  '.#######.',
  '#########',
  '.........',
  '...###...',
]

const PIXELS = BELL.flatMap((row, y) =>
  [...row].map((cell, x) => (cell === '#' ? { x, y } : null)).filter(Boolean),
)

function PixelBell() {
  return (
    <svg className="pixel-bell" viewBox="0 0 9 10" shapeRendering="crispEdges" aria-hidden="true" focusable="false">
      {PIXELS.map(({ x, y }) => (
        <rect key={`${x}-${y}`} x={x} y={y} width="1" height="1" />
      ))}
    </svg>
  )
}

const RELATIVE = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' })
const DATE = new Intl.DateTimeFormat('pt-BR', { day: 'numeric', month: 'short' })

function timeAgo(iso) {
  const seconds = Math.round((new Date(iso).getTime() - Date.now()) / 1000)
  if (seconds > -60) return 'agora'
  if (seconds > -3600) return RELATIVE.format(Math.round(seconds / 60), 'minute')
  if (seconds > -86400) return RELATIVE.format(Math.round(seconds / 3600), 'hour')
  if (seconds > -7 * 86400) return RELATIVE.format(Math.round(seconds / 86400), 'day')
  return DATE.format(new Date(iso))
}

// "curtiu sua avaliação de", "respondeu à sua resposta em"...
const ACTIONS = {
  ReviewLiked: 'curtiu sua avaliação de',
  ReplyLiked: 'curtiu sua resposta em',
  ReviewReplied: 'respondeu à sua avaliação de',
  ReplyReplied: 'respondeu à sua resposta em',
  PlanInviteReceived: 'convidou você para o plano',
  PlanInviteAccepted: 'aceitou seu convite e entrou no plano',
  PlanInviteDeclined: 'recusou seu convite para o plano',
}

// Situação do convite recebido, quando não dá mais para responder
const INVITE_OUTCOMES = {
  Accepted: 'Você aceitou este convite.',
  Declined: 'Você recusou este convite.',
  Closed: 'Este convite não está mais valendo.',
}

function NotificationText({ notification }) {
  const subject = isPlanNotification(notification) ? notification.planName || 'familiar' : notification.movieTitle
  return (
    <>
      <span className="notification-text">
        <strong>{notification.actorName || 'Usuário removido'}</strong> {ACTIONS[notification.type]}{' '}
        <cite>{subject}</cite>
      </span>
      {notification.excerpt && <span className="notification-excerpt">“{notification.excerpt}”</span>}
      <time className="notification-time" dateTime={notification.createdAt}>
        {timeAgo(notification.createdAt)}
      </time>
    </>
  )
}

// Responder o convite direto no sino. Os botões ficam fora do botão do aviso (não dá para aninhar).
function InviteActions({ notification, token, onAnswered }) {
  const [busy, setBusy] = useState('')
  const [error, setError] = useState('')

  if (notification.inviteStatus !== 'Pending') {
    const outcome = INVITE_OUTCOMES[notification.inviteStatus]
    return outcome ? <p className="notification-invite-outcome">{outcome}</p> : null
  }

  async function answer(kind) {
    setBusy(kind)
    setError('')
    try {
      await (kind === 'accept' ? acceptInvite : declineInvite)(notification.inviteId, token)
      onAnswered(notification, kind === 'accept' ? 'Accepted' : 'Declined')
    } catch (requestError) {
      // Convite que venceu ou foi cancelado enquanto o painel estava aberto
      if (requestError.code === 'invite-closed' || requestError.code === 'invite-not-found') {
        onAnswered(notification, 'Closed')
      } else {
        setError(requestError.message)
      }
    } finally {
      setBusy('')
    }
  }

  return (
    <div className="notification-invite-actions">
      <div className="notification-invite-buttons">
        <button type="button" className="btn-primary btn-small" disabled={busy !== ''} onClick={() => answer('accept')}>
          {busy === 'accept' ? 'Aceitando...' : 'Aceitar'}
        </button>
        <button type="button" className="btn-secondary btn-small" disabled={busy !== ''} onClick={() => answer('decline')}>
          {busy === 'decline' ? 'Recusando...' : 'Recusar'}
        </button>
      </div>
      {error && (
        <p className="notification-invite-error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}

// Sino do cabeçalho: contador de não lidas e painel com os avisos.
// onOpen(notification) leva à conversa do aviso (ou à assinatura, nos avisos de plano).
// onInviteAnswered(status) avisa que um convite foi respondido aqui, para a assinatura se atualizar.
export default function NotificationBell({ token, onOpen, onInviteAnswered = () => {} }) {
  const panelId = useId()
  const rootRef = useRef(null)
  const buttonRef = useRef(null)
  const [open, setOpen] = useState(false)
  const [unread, setUnread] = useState(0)
  const [state, setState] = useState({ status: 'idle', items: [], error: '' })

  const refreshCount = useCallback(() => {
    getUnreadNotificationCount(token)
      .then(setUnread)
      .catch(() => {
        // Contador é secundário: um erro aqui não deve atrapalhar a página
      })
  }, [token])

  // Atualiza o contador ao abrir o site, a cada minuto (com a aba visível) e ao voltar para a aba
  useEffect(() => {
    refreshCount()
    const timer = setInterval(() => {
      if (document.visibilityState === 'visible') refreshCount()
    }, POLL_MS)
    const onVisible = () => {
      if (document.visibilityState === 'visible') refreshCount()
    }
    document.addEventListener('visibilitychange', onVisible)
    return () => {
      clearInterval(timer)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [refreshCount])

  const load = useCallback(() => {
    setState((previous) => ({ ...previous, status: 'loading', error: '' }))
    getMyNotifications({ page: 1, pageSize: 20 }, token)
      .then((data) => {
        setState({ status: 'ready', items: data.items, error: '' })
        setUnread(data.unreadCount)
      })
      .catch((requestError) => setState({ status: 'error', items: [], error: requestError.message }))
  }, [token])

  // Fecha com Esc ou clique fora
  useEffect(() => {
    if (!open) return undefined
    function onKey(event) {
      if (event.key === 'Escape') {
        setOpen(false)
        buttonRef.current?.focus()
      }
    }
    function onPointer(event) {
      if (!rootRef.current?.contains(event.target)) setOpen(false)
    }
    document.addEventListener('keydown', onKey)
    document.addEventListener('pointerdown', onPointer)
    return () => {
      document.removeEventListener('keydown', onKey)
      document.removeEventListener('pointerdown', onPointer)
    }
  }, [open])

  function toggle() {
    if (!open) load()
    setOpen(!open)
  }

  async function markAll() {
    setState((previous) => ({ ...previous, items: previous.items.map((item) => ({ ...item, isRead: true })) }))
    setUnread(0)
    try {
      await markAllNotificationsRead(token)
    } catch {
      refreshCount()
    }
  }

  function openNotification(notification) {
    if (!notification.isRead) {
      setUnread((count) => Math.max(0, count - 1))
      markNotificationRead(notification.id, token).catch(() => {})
    }
    setOpen(false)
    onOpen(notification)
  }

  function inviteAnswered(notification, status) {
    if (!notification.isRead) setUnread((count) => Math.max(0, count - 1))
    setState((previous) => ({
      ...previous,
      items: previous.items.map((item) =>
        item.id === notification.id ? { ...item, inviteStatus: status, isRead: true } : item,
      ),
    }))
    // A API já marca o aviso como lido ao responder; esta chamada cobre o convite encerrado
    if (!notification.isRead) markNotificationRead(notification.id, token).catch(() => {})
    if (status !== 'Closed') onInviteAnswered(status)
  }

  const label = unread > 0 ? `Notificações, ${unread} não ${unread === 1 ? 'lida' : 'lidas'}` : 'Notificações'

  return (
    <div className="notifications" ref={rootRef}>
      <button
        ref={buttonRef}
        type="button"
        className={open ? 'notification-button is-open' : 'notification-button'}
        aria-label={label}
        title={label}
        aria-expanded={open}
        aria-controls={panelId}
        onClick={toggle}
      >
        <PixelBell />
        {unread > 0 && (
          <span className="notification-badge" aria-hidden="true">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      {open && (
        <section id={panelId} className="notification-panel" aria-label="Notificações">
          <header className="notification-panel-title">
            <h2>Notificações</h2>
            {state.items.some((item) => !item.isRead) && (
              <button type="button" className="link-button notification-mark-all" onClick={markAll}>
                Marcar todas como lidas
              </button>
            )}
          </header>

          {state.status === 'loading' && state.items.length === 0 && (
            <p className="notification-empty" role="status">
              Carregando...
            </p>
          )}

          {state.status === 'error' && (
            <div className="notification-empty">
              <p role="alert">{state.error}</p>
              <button type="button" className="btn-secondary btn-small" onClick={load}>
                Tentar novamente
              </button>
            </div>
          )}

          {state.status === 'ready' && state.items.length === 0 && (
            <p className="notification-empty">
              Nada por aqui ainda. Curtidas e respostas no que você escreveu e convites para planos familiares
              aparecem aqui.
            </p>
          )}

          {state.items.length > 0 && (
            <ul className="notification-list">
              {state.items.map((notification) => (
                <li key={notification.id} className={notification.isRead ? undefined : 'is-unread'}>
                  <button
                    type="button"
                    className={notification.isRead ? 'notification-item' : 'notification-item is-unread'}
                    onClick={() => openNotification(notification)}
                  >
                    <Avatar name={notification.actorName || '?'} photoUrl={notification.actorPhotoUrl} size={32} />
                    <span className="notification-body">
                      <NotificationText notification={notification} />
                    </span>
                    {!notification.isRead && <span className="visually-hidden"> (não lida)</span>}
                  </button>
                  {notification.type === 'PlanInviteReceived' && (
                    <InviteActions notification={notification} token={token} onAnswered={inviteAnswered} />
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </div>
  )
}
