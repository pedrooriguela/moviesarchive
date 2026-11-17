import { useEffect, useState } from 'react'
import { authFetch, getPhotoSrc } from '../services/api.js'

export default function SharedProfilePage({ token, userId, viewerId, onBack, onOpenMovie }) {
  const [profile, setProfile] = useState(null)
  const [listsPage, setListsPage] = useState(1)
  const [reviewsPage, setReviewsPage] = useState(1)
  const [revision, setRevision] = useState(0)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [listDetails, setListDetails] = useState(null)

  useEffect(() => {
    let active = true
    authFetch(`/api/users/${userId}/profile?listsPage=${listsPage}&reviewsPage=${reviewsPage}`, token)
      .then((data) => { if (active) { setProfile(data); setError(''); setLoading(false) } })
      .catch((requestError) => { if (active) { setProfile(null); setError(requestError.message); setLoading(false) } })
    return () => { active = false }
  }, [token, userId, listsPage, reviewsPage, revision])

  async function toggleFollow() {
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await authFetch(`/api/users/${userId}/follow`, token, { method: profile.isFollowing || profile.isRequested ? 'DELETE' : 'PUT' })
      setNotice(profile.isRequested ? 'Solicitação cancelada.' : profile.isFollowing ? 'Você deixou de seguir este perfil.'
        : profile.isPrivate ? 'Solicitação enviada. Aguarde a aprovação.' : 'Você está seguindo este perfil.')
      setListDetails(null)
      setLoading(true)
      setRevision((v) => v + 1)
    } catch (requestError) { setError(requestError.message) }
    finally { setBusy(false) }
  }

  async function openList(id) {
    setBusy(true)
    setError('')
    setListDetails(null)
    try { setListDetails(await authFetch(`/api/lists/${id}`, token)) }
    catch (requestError) { setError(requestError.message) }
    finally { setBusy(false) }
  }

  function paginate(kind, page) {
    setLoading(true)
    setListDetails(null)
    if (kind === 'lists') setListsPage(page)
    else setReviewsPage(page)
  }

  return <section className="window users-window" aria-labelledby="shared-profile-title">
    <div className="window-title"><h1 id="shared-profile-title">Perfil compartilhado</h1></div>
    <nav className="menubar"><button type="button" onClick={onBack}>Voltar à comunidade</button></nav>
    <div className="window-body">
      {error && <div role="alert"><p>{error}</p><button type="button" className="btn-secondary" onClick={() => { setLoading(true); setRevision((v) => v + 1) }}>Atualizar perfil</button></div>}
      {notice && <p role="status">{notice}</p>}
      {loading ? <p role="status">Carregando perfil...</p> : profile && <>
        <div className="social-user">
          {profile.photoUrl && <img src={getPhotoSrc(profile.photoUrl)} alt="" />}
          <h2>{profile.name}</h2>
          <span>{profile.isPrivate ? 'Perfil privado' : 'Perfil público'}</span>
        </div>
        <dl className="profile-counters">
          <div><dt>Avaliações</dt><dd>{profile.reviewsTotal}</dd></div>
          <div><dt>Listas públicas</dt><dd>{profile.listsTotal}</dd></div>
          <div><dt>Seguidores</dt><dd>{profile.followersCount}</dd></div>
          <div><dt>Seguindo</dt><dd>{profile.followingCount}</dd></div>
        </dl>
        {userId !== viewerId && <button type="button" className="btn-primary" disabled={busy} onClick={toggleFollow}>
          {profile.isRequested ? 'Cancelar solicitação' : profile.isFollowing ? 'Deixar de seguir' : profile.isPrivate ? 'Pedir para seguir' : 'Seguir'}
        </button>}
        {!profile.canViewContent ? <p>Este perfil é privado. Apenas seguidores aprovados podem ver suas listas públicas e avaliações.</p> : <>
          <h3>Listas públicas</h3>
          {profile.lists.length === 0 && <p>Nenhuma lista nesta página.</p>}
          <ul className="shared-content">{profile.lists.map((list) => <li key={list.id}>
            <button type="button" className="social-profile-link" disabled={busy} onClick={() => openList(list.id)}>{list.title}</button>
            <p>{list.description}</p><span>{list.movieCount} filmes</span>
          </li>)}</ul>
          {profile.listsTotal > 10 && <nav className="pagination" aria-label="Páginas de listas">
            <button type="button" className="btn-secondary" disabled={listsPage === 1 || busy} onClick={() => paginate('lists', listsPage - 1)}>Anterior</button>
            <span>Página {listsPage}</span>
            <button type="button" className="btn-secondary" disabled={listsPage * 10 >= profile.listsTotal || busy} onClick={() => paginate('lists', listsPage + 1)}>Próxima</button>
          </nav>}
          {listDetails && <div className="shared-list-details"><h4>{listDetails.title}</h4>
            <ul>{listDetails.items.map((item) => <li key={item.movieId}><button type="button" className="social-profile-link" onClick={() => onOpenMovie(item.movieId)}>{item.movieTitle}</button></li>)}</ul>
            <button type="button" className="btn-secondary" onClick={() => setListDetails(null)}>Fechar lista</button>
          </div>}
          <h3>Avaliações</h3>
          {profile.reviews.length === 0 && <p>Nenhuma avaliação nesta página.</p>}
          <ul className="shared-content">{profile.reviews.map((review) => <li key={review.id}>
            <button type="button" className="social-profile-link" onClick={() => onOpenMovie(review.movieId)}>{review.movieTitle}</button>
            <p>Nota: {review.rating} / 5</p><p>{review.comment}</p>
          </li>)}</ul>
          {profile.reviewsTotal > 10 && <nav className="pagination" aria-label="Páginas de avaliações">
            <button type="button" className="btn-secondary" disabled={reviewsPage === 1 || busy} onClick={() => paginate('reviews', reviewsPage - 1)}>Anterior</button>
            <span>Página {reviewsPage}</span>
            <button type="button" className="btn-secondary" disabled={reviewsPage * 10 >= profile.reviewsTotal || busy} onClick={() => paginate('reviews', reviewsPage + 1)}>Próxima</button>
          </nav>}
        </>}
      </>}
    </div>
  </section>
}
