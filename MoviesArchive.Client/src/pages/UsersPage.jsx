import { useEffect, useState } from 'react'
import { authFetch, getPhotoSrc } from '../services/api.js'

const PAGE_SIZE = 20

export default function UsersPage({ token, onBack, onOpenUser }) {
  const [search, setSearch] = useState('')
  const [scope, setScope] = useState('all')
  const [offset, setOffset] = useState(0)
  const [revision, setRevision] = useState(0)
  const [result, setResult] = useState({ loading: true, users: [], error: '' })
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')

  useEffect(() => {
    let active = true
    const timer = setTimeout(() => {
      setResult({ loading: true, users: [], error: '' })
      const params = new URLSearchParams({ search, scope, offset, limit: PAGE_SIZE })
      authFetch(scope === 'requests' ? `/api/users/me/follow-requests?offset=${offset}&limit=${PAGE_SIZE}` : `/api/users?${params}`, token)
        .then((users) => { if (active) setResult({ loading: false, users, error: '' }) })
        .catch((error) => { if (active) setResult({ loading: false, users: [], error: error.message }) })
    }, 300)
    return () => { active = false; clearTimeout(timer) }
  }, [token, search, scope, offset, revision])

  async function toggleFollow(person) {
    setBusy(true)
    setMessage('')
    try {
      await authFetch(`/api/users/${person.id}/follow`, token, {
        method: person.isFollowing || person.isRequested ? 'DELETE' : 'PUT',
      })
      setMessage(person.isRequested ? `Solicitação para ${person.name} cancelada.` : person.isFollowing
        ? `Você deixou de seguir ${person.name}.` : person.isPrivate
          ? `Solicitação enviada para ${person.name}.` : `Você está seguindo ${person.name}.`)
      setResult({ loading: true, users: [], error: '' })
      setRevision((value) => value + 1)
    } catch (error) {
      setMessage(error.message)
    } finally {
      setBusy(false)
    }
  }

  async function answerRequest(person, accept) {
    setBusy(true)
    setMessage('')
    try {
      await authFetch(`/api/users/me/follow-requests/${person.id}`, token, { method: accept ? 'PUT' : 'DELETE' })
      setMessage(accept ? `Solicitação de ${person.name} aprovada.` : `Solicitação de ${person.name} recusada.`)
      setResult({ loading: true, users: [], error: '' })
      setRevision((value) => value + 1)
    } catch (error) { setMessage(error.message) }
    finally { setBusy(false) }
  }

  return (
    <section className="window users-window" aria-labelledby="users-title">
      <div className="window-title"><h1 id="users-title">Comunidade</h1></div>
      <nav className="menubar" aria-label="Navegação da comunidade">
        <button type="button" onClick={onBack}>Voltar ao início</button>
      </nav>
      <div className="window-body">
        <p>Encontre pessoas que também gostam de cinema.</p>
        <div className="field">
          <label htmlFor="users-search">Buscar por nome de usuário</label>
          <input id="users-search" type="search" maxLength={100} value={search} disabled={busy || scope === 'requests'}
            onChange={(event) => { setSearch(event.target.value); setOffset(0) }} />
        </div>
        <div className="field">
          <label htmlFor="users-scope">Mostrar</label>
          <select id="users-scope" value={scope} disabled={busy}
            onChange={(event) => { setScope(event.target.value); setOffset(0); setMessage('') }}>
            <option value="all">Todas as pessoas</option>
            <option value="following">Seguindo</option>
            <option value="followers">Meus seguidores</option>
            <option value="requests">Solicitações recebidas</option>
          </select>
        </div>
        <p role="status">{message}</p>
        {result.loading ? <p role="status">Carregando pessoas...</p> : result.error ? (
          <div role="alert">
            <p>{result.error}</p>
            <button type="button" className="btn-secondary" onClick={() => setRevision((v) => v + 1)}>Tentar novamente</button>
          </div>
        ) : (
          <>
            {result.users.length === 0 && <p>Nenhum usuário encontrado nesta página.</p>}
            <ul className="social-users">
              {result.users.map((person) => (
                <li key={person.id} className="social-user">
                  {person.photoUrl ? <img src={getPhotoSrc(person.photoUrl)} alt="" /> :
                    <span className="social-avatar" aria-hidden="true">{person.name.charAt(0).toUpperCase()}</span>}
                  <button type="button" className="social-name social-profile-link" onClick={() => onOpenUser(person.id)}>
                    {person.name}{person.isPrivate ? ' · Privado' : ''}
                  </button>
                  {scope === 'requests' ? <div className="social-actions">
                    <button type="button" className="btn-primary" disabled={busy} onClick={() => answerRequest(person, true)}>Aprovar</button>
                    <button type="button" className="btn-secondary" disabled={busy} onClick={() => answerRequest(person, false)}>Recusar</button>
                  </div> : <button type="button" className={person.isFollowing || person.isRequested ? 'btn-secondary' : 'btn-primary'}
                    disabled={busy} onClick={() => toggleFollow(person)}
                    aria-label={`${person.isRequested ? 'Cancelar solicitação para' : person.isFollowing ? 'Deixar de seguir' : person.isPrivate ? 'Pedir para seguir' : 'Seguir'} ${person.name}`}>
                    {person.isRequested ? 'Cancelar solicitação' : person.isFollowing ? 'Deixar de seguir' : person.isPrivate ? 'Pedir para seguir' : 'Seguir'}
                  </button>}
                </li>
              ))}
            </ul>
            <nav className="pagination" aria-label="Páginas de usuários">
              <button type="button" className="btn-secondary" disabled={busy || offset === 0}
                onClick={() => setOffset((v) => Math.max(0, v - PAGE_SIZE))}>Anterior</button>
              <span>Página {offset / PAGE_SIZE + 1}</span>
              <button type="button" className="btn-secondary" disabled={busy || result.users.length < PAGE_SIZE}
                onClick={() => setOffset((v) => v + PAGE_SIZE)}>Próxima</button>
            </nav>
          </>
        )}
      </div>
    </section>
  )
}
