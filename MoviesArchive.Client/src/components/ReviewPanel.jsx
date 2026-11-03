import { useState } from 'react'
import StarRatingInput from './StarRatingInput.jsx'
import { MAX_COMMENT_LENGTH, deleteMyReview, saveMyReview, setInWatchlist } from '../services/reviews.js'

// Painel lateral da ficha: watchlist + nota + comentário do usuário logado.
// "state" é o MovieUserStateDto ({ inWatchlist, review }) já carregado pela página.
export default function ReviewPanel({ movieId, token, state, onStateChange, onReviewChanged }) {
  const saved = state.review
  const [rating, setRating] = useState(saved?.rating ?? null)
  const [comment, setComment] = useState(saved?.comment ?? '')
  const [busy, setBusy] = useState(null) // null | 'watchlist' | 'save' | 'delete'
  const [error, setError] = useState('')
  const [ratingError, setRatingError] = useState('')
  const [notice, setNotice] = useState('')

  const dirty = rating !== (saved?.rating ?? null) || comment.trim() !== (saved?.comment ?? '')
  const remaining = MAX_COMMENT_LENGTH - comment.length

  async function toggleWatchlist() {
    const next = !state.inWatchlist
    setBusy('watchlist')
    setError('')
    setNotice('')
    try {
      await setInWatchlist(movieId, next, token)
      onStateChange({ ...state, inWatchlist: next })
      setNotice(next ? 'Adicionado à sua watchlist.' : 'Removido da sua watchlist.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusy(null)
    }
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setNotice('')

    if (!rating) {
      setRatingError('Escolha uma nota de meia a cinco estrelas.')
      return
    }
    setRatingError('')

    setBusy('save')
    try {
      const review = await saveMyReview(movieId, { rating, comment }, token)
      setRating(review.rating)
      setComment(review.comment ?? '')
      onStateChange({ ...state, review })
      onReviewChanged()
      setNotice(saved ? 'Avaliação atualizada.' : 'Avaliação publicada.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusy(null)
    }
  }

  async function handleDelete() {
    if (!window.confirm('Excluir sua avaliação deste filme?')) return
    setBusy('delete')
    setError('')
    setNotice('')
    try {
      await deleteMyReview(movieId, token)
      setRating(null)
      setComment('')
      onStateChange({ ...state, review: null })
      onReviewChanged()
      setNotice('Avaliação excluída.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusy(null)
    }
  }

  function discardChanges() {
    setRating(saved?.rating ?? null)
    setComment(saved?.comment ?? '')
    setRatingError('')
    setError('')
  }

  return (
    <aside className="review-panel" aria-labelledby="painel-titulo">
      <h2 id="painel-titulo" className="review-panel-title">
        Seu registro
      </h2>

      <button
        type="button"
        className={state.inWatchlist ? 'watchlist-toggle is-on' : 'watchlist-toggle'}
        aria-pressed={state.inWatchlist}
        onClick={toggleWatchlist}
        disabled={busy !== null}
      >
        <span className="watchlist-box" aria-hidden="true">
          {state.inWatchlist ? '✓' : ''}
        </span>
        <span>
          {state.inWatchlist ? 'Na sua watchlist' : 'Adicionar à watchlist'}
          <small>{state.inWatchlist ? 'Clique para remover' : 'Para assistir depois'}</small>
        </span>
      </button>

      <form className="review-form" onSubmit={handleSubmit} noValidate>
        <div className="field">
          {/* O seletor é um role="slider", então o rótulo é ligado por aria-labelledby */}
          <span id="sua-nota-rotulo" className="field-label" onClick={() => document.getElementById('sua-nota')?.focus()}>
            {saved ? 'Sua nota' : 'Dê sua nota'}
          </span>
          <StarRatingInput
            id="sua-nota"
            labelledBy="sua-nota-rotulo"
            value={rating}
            onChange={(next) => {
              setRating(next)
              if (next) setRatingError('')
            }}
            disabled={busy !== null}
            invalid={Boolean(ratingError)}
            describedBy={ratingError ? 'sua-nota-erro' : 'sua-nota-dica'}
          />
          {ratingError ? (
            <span id="sua-nota-erro" className="field-error">
              {ratingError}
            </span>
          ) : (
            <span id="sua-nota-dica" className="field-hint">
              Clique na metade esquerda da estrela para meia estrela.
            </span>
          )}
        </div>

        <div className="field">
          <label htmlFor="seu-comentario">Comentário (opcional)</label>
          <textarea
            id="seu-comentario"
            rows={5}
            maxLength={MAX_COMMENT_LENGTH}
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            placeholder="O que ficou com você depois da sessão?"
            disabled={busy !== null}
            aria-describedby="seu-comentario-contador"
          />
          <span id="seu-comentario-contador" className={remaining < 100 ? 'field-hint is-low' : 'field-hint'}>
            {remaining} caracteres restantes
          </span>
        </div>

        {error && (
          <div className="alert alert-error" role="alert">
            {error}
          </div>
        )}
        {notice && (
          <div className="alert alert-success" role="status">
            {notice}
          </div>
        )}

        <div className="review-form-actions">
          <button type="submit" className="btn-primary" disabled={busy !== null || (saved && !dirty)}>
            {busy === 'save' ? 'Salvando...' : saved ? 'Atualizar avaliação' : 'Publicar avaliação'}
          </button>
          {saved && dirty && (
            <button type="button" className="btn-secondary" onClick={discardChanges} disabled={busy !== null}>
              Desfazer alterações
            </button>
          )}
          {saved && !dirty && (
            <button type="button" className="btn-danger" onClick={handleDelete} disabled={busy !== null}>
              {busy === 'delete' ? 'Excluindo...' : 'Excluir avaliação'}
            </button>
          )}
        </div>
      </form>
    </aside>
  )
}
