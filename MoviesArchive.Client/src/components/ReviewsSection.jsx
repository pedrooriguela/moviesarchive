import { useState } from 'react'
import Avatar from './Avatar.jsx'
import LikeButton from './LikeButton.jsx'
import ReviewThread from './ReviewThread.jsx'
import { PixelStarRow } from './PixelStar.jsx'
import { setReviewLike } from '../services/reviews.js'
import { RATING_STEPS, formatRating } from '../services/reviews.js'

const DATE_FORMAT = new Intl.DateTimeFormat('pt-BR', { day: 'numeric', month: 'short', year: 'numeric' })

function plural(count, one, many) {
  return `${count} ${count === 1 ? one : many}`
}

// Distribuição das notas: uma barra por meia estrela, de 0,5 a 5
function Histogram({ histogram }) {
  const max = Math.max(1, ...histogram)

  return (
    <div className="histogram">
      <span className="histogram-edge" aria-hidden="true">
        ½
      </span>
      <ul className="histogram-bars" aria-label="Distribuição das notas">
        {RATING_STEPS.map((step, index) => {
          const count = histogram[index] ?? 0
          return (
            <li
              key={step}
              className="histogram-bar"
              style={{ '--h': `${Math.round((count / max) * 100)}%` }}
              title={`${formatRating(step)} ★ · ${plural(count, 'avaliação', 'avaliações')}`}
            >
              <span className="visually-hidden">
                {formatRating(step)} estrelas: {plural(count, 'avaliação', 'avaliações')}
              </span>
            </li>
          )
        })}
      </ul>
      <span className="histogram-edge" aria-hidden="true">
        5★
      </span>
    </div>
  )
}

function ReviewItem({ review, isMine, token, userId }) {
  const [threadOpen, setThreadOpen] = useState(false)
  const [replyCount, setReplyCount] = useState(review.replyCount ?? 0)
  const threadId = `respostas-${review.id}`

  return (
    <li className={isMine ? 'review is-mine' : 'review'}>
      <Avatar name={review.userName} photoUrl={review.userPhotoUrl} />
      <div className="review-body">
        <p className="review-meta">
          <span className="review-author">{review.userName}</span>
          {isMine && <span className="badge">Você</span>}
          <span className="review-stars" role="img" aria-label={`Nota ${formatRating(review.rating)} de 5`}>
            <PixelStarRow value={review.rating} />
          </span>
          <time className="review-date" dateTime={review.createdAt}>
            {DATE_FORMAT.format(new Date(review.createdAt))}
          </time>
        </p>
        {review.comment ? (
          <p className="review-comment">{review.comment}</p>
        ) : (
          <p className="review-comment is-empty">Avaliou sem comentar.</p>
        )}

        <div className="review-actions">
          <LikeButton
            liked={review.likedByMe ?? false}
            count={review.likeCount ?? 0}
            subject={isMine ? 'sua avaliação' : `a avaliação de ${review.userName}`}
            onChange={(liked) => setReviewLike(review.id, liked, token)}
          />
          <button
            type="button"
            className="link-button"
            aria-expanded={threadOpen}
            aria-controls={threadId}
            onClick={() => setThreadOpen((open) => !open)}
          >
            {threadOpen
              ? 'Fechar respostas'
              : replyCount > 0
                ? `Ver ${plural(replyCount, 'resposta', 'respostas')}`
                : 'Responder'}
          </button>
        </div>

        {threadOpen && (
          <ReviewThread id={threadId} review={review} token={token} userId={userId} onCountChange={setReplyCount} />
        )}
      </div>
    </li>
  )
}

// Média, distribuição e lista de avaliações do filme
export default function ReviewsSection({ data, status, error, userId, token, onRetry, onLoadMore, loadingMore }) {
  return (
    <section className="reviews" aria-labelledby="avaliacoes-titulo">
      <h2 id="avaliacoes-titulo" className="movie-section-title">
        Avaliações dos espectadores
      </h2>

      {status === 'loading' && !data && (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Carregando avaliações...</span>
        </div>
      )}

      {status === 'error' && (
        <div className="results-state">
          <div className="alert alert-error" role="alert">
            {error}
          </div>
          <button type="button" className="btn-secondary" onClick={onRetry}>
            Tentar novamente
          </button>
        </div>
      )}

      {data && (
        <>
          <div className="reviews-summary">
            <div className="reviews-average">
              <span className="reviews-average-number">
                {data.averageRating != null ? formatRating(Math.round(data.averageRating * 10) / 10) : '–'}
              </span>
              <span className="reviews-average-caption">
                {data.totalCount
                  ? `média de ${plural(data.totalCount, 'avaliação', 'avaliações')}`
                  : 'ainda sem avaliações'}
              </span>
            </div>
            <Histogram histogram={data.histogram} />
          </div>

          {data.items.length === 0 ? (
            <p className="reviews-empty">Ninguém avaliou este filme ainda. A primeira nota pode ser a sua.</p>
          ) : (
            <ul className="review-list">
              {data.items.map((review) => (
                <ReviewItem
                  key={review.id}
                  review={review}
                  isMine={review.userId === userId}
                  token={token}
                  userId={userId}
                />
              ))}
            </ul>
          )}

          {data.items.length < data.totalCount && (
            <button type="button" className="btn-secondary reviews-more" onClick={onLoadMore} disabled={loadingMore}>
              {loadingMore ? 'Carregando...' : `Ver mais avaliações (${data.totalCount - data.items.length})`}
            </button>
          )}
        </>
      )}
    </section>
  )
}
