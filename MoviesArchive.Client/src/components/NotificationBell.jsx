import { useCallback, useEffect, useId, useRef, useState } from 'react'
import Avatar from './Avatar.jsx'
import {
  getMyNotifications,
  getUnreadNotificationCount,
  markAllNotificationsRead,
  markNotificationRead,
} from '../services/notifications.js'

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
}

function NotificationText({ notification }) {
  return (
    <>
      <span className="notification-text">
        <strong>{notification.actorName || 'Usuário removido'}</strong> {ACTIONS[notification.type]}{' '}
        <cite>{notification.movieTitle}</cite>
      </span>
      {notification.excerpt && <span className="notification-excerpt">“{notification.excerpt}”</span>}
      <time className="notification-time" dateTime={notification.createdAt}>
        {timeAgo(notification.createdAt)}
      </time>
    </>
  )
}

// Sino do cabeçalho: contador de não lidas e painel com os avisos.
// onOpen(notification) leva à conversa do aviso.
export default function NotificationBell({ token, onOpen }) {
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
              Nada por aqui ainda. Quando alguém curtir ou responder uma avaliação ou resposta sua, o aviso aparece aqui.
            </p>
          )}

          {state.items.length > 0 && (
            <ul className="notification-list">
              {state.items.map((notification) => (
                <li key={notification.id}>
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
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </div>
  )
}
