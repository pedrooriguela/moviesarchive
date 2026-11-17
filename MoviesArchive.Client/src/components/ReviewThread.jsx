import { useEffect, useMemo, useState } from 'react'
import Avatar from './Avatar.jsx'
import LikeButton from './LikeButton.jsx'
import { MAX_REPLY_LENGTH, addReply, deleteReply, getReplies, setReplyLike } from '../services/reviews.js'

const DATE_FORMAT = new Intl.DateTimeFormat('pt-BR', { day: 'numeric', month: 'short', year: 'numeric' })

// A partir desta profundidade as respostas param de recuar, para caber no celular
const MAX_INDENT = 3

// Monta a árvore pelo parentReplyId. Resposta cujo "pai" sumiu vira resposta direta à avaliação.
function buildTree(replies) {
  const nodes = new Map(replies.map((reply) => [reply.id, { reply, children: [] }]))
  const roots = []
  for (const node of nodes.values()) {
    const parent = node.reply.parentReplyId ? nodes.get(node.reply.parentReplyId) : null
    if (parent) parent.children.push(node)
    else roots.push(node)
  }
  return roots
}

function ReplyForm({ id, label, placeholder, autoFocus, onSubmit, onCancel }) {
  const [text, setText] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const remaining = MAX_REPLY_LENGTH - text.length

  async function handleSubmit(event) {
    event.preventDefault()
    if (!text.trim()) {
      setError('Escreva a resposta antes de enviar.')
      return
    }
    setBusy(true)
    setError('')
    try {
      await onSubmit(text)
      setText('')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="reply-form" onSubmit={handleSubmit} noValidate>
      <label className="visually-hidden" htmlFor={id}>
        {label}
      </label>
      <textarea
        id={id}
        rows={3}
        maxLength={MAX_REPLY_LENGTH}
        value={text}
        placeholder={placeholder}
        autoFocus={autoFocus}
        disabled={busy}
        aria-invalid={Boolean(error) || undefined}
        aria-describedby={`${id}-ajuda`}
        onChange={(event) => setText(event.target.value)}
      />
      <div className="reply-form-footer">
        <span id={`${id}-ajuda`} className={error ? 'field-error' : remaining < 100 ? 'field-hint is-low' : 'field-hint'}>
          {error || `${remaining} caracteres restantes`}
        </span>
        <span className="reply-form-actions">
          {onCancel && (
            <button type="button" className="btn-secondary btn-small" onClick={onCancel} disabled={busy}>
              Cancelar
            </button>
          )}
          <button type="submit" className="btn-primary btn-small" disabled={busy}>
            {busy ? 'Enviando...' : 'Enviar resposta'}
          </button>
        </span>
      </div>
    </form>
  )
}

function ReplyNode({ node, depth, parentName, userId, token, replyingTo, setReplyingTo, onReply, onDelete }) {
  const { reply, children } = node
  const isMine = reply.userId === userId
  const formId = `responder-${reply.id}`
  const [deleting, setDeleting] = useState(false)

  async function handleDelete() {
    if (!window.confirm('Excluir esta resposta?')) return
    setDeleting(true)
    try {
      await onDelete(reply.id)
    } finally {
      setDeleting(false)
    }
  }

  return (
    <li className={isMine ? 'reply is-mine' : 'reply'}>
      <div className="reply-card">
        {reply.isDeleted ? (
          <span className="reply-avatar-gap" aria-hidden="true" />
        ) : (
          <Avatar name={reply.userName} photoUrl={reply.userPhotoUrl} size={28} />
        )}
        <div className="reply-body">
          {reply.isDeleted ? (
            <p className="reply-comment is-deleted">Resposta excluída.</p>
          ) : (
            <>
              <p className="reply-meta">
                <span className="review-author">{reply.userName}</span>
                {isMine && <span className="badge">Você</span>}
                {/* Recuo limitado: mais fundo que isso, diz a quem responde */}
                {depth >= MAX_INDENT && parentName && <span className="reply-to">em resposta a {parentName}</span>}
                <time className="review-date" dateTime={reply.createdAt}>
                  {DATE_FORMAT.format(new Date(reply.createdAt))}
                </time>
              </p>
              <p className="reply-comment">{reply.comment}</p>
              <div className="reply-actions">
                <LikeButton
                  liked={reply.likedByMe}
                  count={reply.likeCount}
                  subject={`a resposta de ${reply.userName}`}
                  onChange={(liked) => setReplyLike(reply.id, liked, token)}
                />
                <button
                  type="button"
                  className="link-button"
                  aria-expanded={replyingTo === reply.id}
                  aria-controls={formId}
                  onClick={() => setReplyingTo(replyingTo === reply.id ? null : reply.id)}
                >
                  Responder
                </button>
                {isMine && (
                  <button type="button" className="link-button is-danger" onClick={handleDelete} disabled={deleting}>
                    {deleting ? 'Excluindo...' : 'Excluir'}
                  </button>
                )}
              </div>
            </>
          )}

          {replyingTo === reply.id && (
            <ReplyForm
              id={formId}
              label={`Responder a ${reply.userName}`}
              placeholder={`Responder a ${reply.userName}...`}
              autoFocus
              onSubmit={(text) => onReply(text, reply.id)}
              onCancel={() => setReplyingTo(null)}
            />
          )}
        </div>
      </div>

      {children.length > 0 && (
        <ul className={depth + 1 >= MAX_INDENT ? 'thread-list is-flat' : 'thread-list is-nested'}>
          {children.map((child) => (
            <ReplyNode
              key={child.reply.id}
              node={child}
              depth={depth + 1}
              parentName={reply.isDeleted ? null : reply.userName}
              userId={userId}
              token={token}
              replyingTo={replyingTo}
              setReplyingTo={setReplyingTo}
              onReply={onReply}
              onDelete={onDelete}
            />
          ))}
        </ul>
      )}
    </li>
  )
}

// Conversa de uma avaliação: respostas, respostas às respostas, curtidas e o campo para responder.
// onCountChange recebe o total de respostas visíveis (sem as excluídas).
export default function ReviewThread({ id, review, token, userId, onCountChange }) {
  const [state, setState] = useState({ status: 'loading', replies: [], error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [replyingTo, setReplyingTo] = useState(null)
  const [actionError, setActionError] = useState('')

  useEffect(() => {
    let active = true
    getReplies(review.id, token)
      .then((replies) => {
        if (active) setState({ status: 'ready', replies, error: '' })
      })
      .catch((requestError) => {
        if (active) setState({ status: 'error', replies: [], error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [review.id, token, reloadKey])

  const tree = useMemo(() => buildTree(state.replies), [state.replies])

  function updateReplies(replies) {
    setState({ status: 'ready', replies, error: '' })
    onCountChange?.(replies.filter((reply) => !reply.isDeleted).length)
  }

  async function handleReply(text, parentReplyId = null) {
    const created = await addReply(review.id, { comment: text, parentReplyId }, token)
    updateReplies([...state.replies, created])
    setReplyingTo(null)
  }

  async function handleDelete(replyId) {
    setActionError('')
    try {
      await deleteReply(replyId, token)
      // Recarrega: a API decide se a resposta some ou vira "resposta excluída"
      const replies = await getReplies(review.id, token)
      updateReplies(replies)
    } catch (requestError) {
      setActionError(requestError.message)
    }
  }

  return (
    <div className="thread" id={id}>
      {state.status === 'loading' && (
        <p className="field-hint" role="status">
          Carregando respostas...
        </p>
      )}

      {state.status === 'error' && (
        <div className="thread-error">
          <div className="alert alert-error" role="alert">
            {state.error}
          </div>
          <button
            type="button"
            className="btn-secondary btn-small"
            onClick={() => {
              setState({ status: 'loading', replies: [], error: '' })
              setReloadKey((key) => key + 1)
            }}
          >
            Tentar novamente
          </button>
        </div>
      )}

      {actionError && (
        <div className="alert alert-error" role="alert">
          {actionError}
        </div>
      )}

      {state.status === 'ready' && tree.length > 0 && (
        <ul className="thread-list">
          {tree.map((node) => (
            <ReplyNode
              key={node.reply.id}
              node={node}
              depth={0}
              parentName={review.userName}
              userId={userId}
              token={token}
              replyingTo={replyingTo}
              setReplyingTo={setReplyingTo}
              onReply={handleReply}
              onDelete={handleDelete}
            />
          ))}
        </ul>
      )}

      {state.status === 'ready' && (
        <ReplyForm
          id={`responder-avaliacao-${review.id}`}
          label={`Responder à avaliação de ${review.userName}`}
          placeholder={`Responder a ${review.userName}...`}
          onSubmit={(text) => handleReply(text, null)}
        />
      )}
    </div>
  )
}
