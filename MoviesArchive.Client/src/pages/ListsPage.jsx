import { useEffect, useState } from 'react'
import {
  addListItem,
  createList,
  deleteList,
  getMyLists,
  removeListItem,
  searchMovies,
  updateList,
  updateListItemsOrder,
} from '../services/api.js'

const EMPTY_FORM = { title: '', description: '', isPublic: false }

// A watchlist (isWatchlist) é a primeira lista e funciona como as outras: só não pode ser
// renomeada nem excluída. O botão "Adicionar à watchlist" da ficha coloca o filme no fim dela.
export default function ListsPage({ token, initialListId, onSelectList, onBack, onOpenMovie }) {
  const [lists, setLists] = useState([])
  const [selectedId, setSelectedId] = useState(null)
  const [mode, setMode] = useState('view') // 'view' | 'create' | 'edit'
  const [form, setForm] = useState(EMPTY_FORM)
  const [search, setSearch] = useState('')
  const [results, setResults] = useState([])
  const [searching, setSearching] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [saving, setSaving] = useState(false)

  const selectedList = lists.find((list) => list.id === selectedId) ?? null

  useEffect(() => {
    let active = true

    getMyLists(token)
      .then((data) => {
        if (!active) return
        setLists(data)
        // Volta para a lista que estava aberta (ex.: depois de abrir a ficha de um filme dela)
        const initial = data.find((list) => list.id === initialListId) ?? data[0]
        setSelectedId(initial?.id ?? null)
        if (data.length === 0) setMode('create')
      })
      .catch((requestError) => {
        if (active) setLoadError(requestError.message)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
    // initialListId só vale na primeira carga; depois, a lista aberta fica no estado desta tela
  }, [token, reloadKey])

  // Guarda a lista aberta para quem chamou a tela (App), para voltar a ela depois da ficha
  useEffect(() => {
    if (selectedId) onSelectList?.(selectedId)
  }, [selectedId, onSelectList])

  function reload() {
    setLoadError('')
    setLoading(true)
    setReloadKey((key) => key + 1)
  }

  // Busca de filmes com atraso, para não chamar a API a cada tecla
  useEffect(() => {
    const term = search.trim()
    if (!term) return

    let active = true
    const timer = setTimeout(() => {
      setSearching(true)
      searchMovies(token, term)
        .then((data) => {
          if (active) setResults(data)
        })
        .catch((requestError) => {
          if (active) setError(requestError.message)
        })
        .finally(() => {
          if (active) setSearching(false)
        })
    }, 300)

    return () => {
      active = false
      clearTimeout(timer)
    }
  }, [search, token])

  function replaceList(updated) {
    setLists((current) => current.map((list) => (list.id === updated.id ? updated : list)))
  }

  // Executa uma ação na API mostrando erro/sucesso de forma padronizada
  async function run(action, successMessage) {
    setError('')
    setNotice('')
    setSaving(true)
    try {
      await action()
      if (successMessage) setNotice(successMessage)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setSaving(false)
    }
  }

  function selectList(listId) {
    setSelectedId(listId)
    setMode('view')
    setSearch('')
    setError('')
    setNotice('')
  }

  function startCreating() {
    setForm(EMPTY_FORM)
    setMode('create')
    setError('')
    setNotice('')
  }

  function startEditing() {
    setForm({
      title: selectedList.title,
      description: selectedList.description ?? '',
      isPublic: selectedList.isPublic,
    })
    setMode('edit')
    setError('')
    setNotice('')
  }

  function cancelForm() {
    setMode(selectedList ? 'view' : 'create')
    setError('')
  }

  function handleSubmit(event) {
    event.preventDefault()

    const payload = { ...form, title: form.title.trim(), description: form.description.trim() }
    // O nome da watchlist não muda: vai sempre o atual
    if (mode === 'edit' && selectedList?.isWatchlist) payload.title = selectedList.title
    if (!payload.title) {
      setError('O título da lista é obrigatório.')
      return
    }

    if (mode === 'create') {
      run(async () => {
        const created = await createList(token, payload)
        // A watchlist continua sempre em primeiro
        setLists((current) => {
          const watchlists = current.filter((list) => list.isWatchlist)
          const others = current.filter((list) => !list.isWatchlist)
          return [...watchlists, created, ...others]
        })
        setSelectedId(created.id)
        setMode('view')
      }, 'Lista criada.')
    } else {
      run(async () => {
        replaceList(await updateList(token, selectedList.id, payload))
        setMode('view')
      }, 'Lista atualizada.')
    }
  }

  function handleDelete() {
    if (selectedList.isWatchlist) return
    if (!window.confirm(`Excluir a lista "${selectedList.title}"?`)) return

    run(async () => {
      await deleteList(token, selectedList.id)
      const remaining = lists.filter((list) => list.id !== selectedList.id)
      setLists(remaining)
      setSelectedId(remaining[0]?.id ?? null)
      setMode(remaining.length ? 'view' : 'create')
    }, 'Lista excluída.')
  }

  function handleAdd(movie) {
    run(async () => {
      replaceList(await addListItem(token, selectedList.id, movie.id))
      setSearch('')
    }, `"${movie.title}" adicionado à lista.`)
  }

  function handleRemove(item) {
    run(async () => {
      replaceList(await removeListItem(token, selectedList.id, item.movieId))
    }, `"${item.movieTitle}" removido da lista.`)
  }

  function handleMove(index, offset) {
    const movieIds = selectedList.items.map((item) => item.movieId)
    const target = index + offset
    ;[movieIds[index], movieIds[target]] = [movieIds[target], movieIds[index]]

    run(async () => {
      replaceList(await updateListItemsOrder(token, selectedList.id, movieIds))
    })
  }

  const listMovieIds = new Set(selectedList?.items.map((item) => item.movieId))

  const editingWatchlist = mode === 'edit' && selectedList?.isWatchlist

  return (
    <section className="profile-layout lists-layout">
      <div className="profile-heading">
        <button type="button" className="back-button" onClick={onBack} aria-label="Voltar ao perfil">
          ← Voltar
        </button>
        <div>
          <h1>Minhas listas</h1>
        </div>
      </div>

      {loading ? (
        <p role="status" className="lists-status">Carregando listas...</p>
      ) : loadError ? (
        // Sem as listas carregadas não dá para editar nada, então só oferece tentar de novo
        <div className="lists-status">
          <div className="alert alert-error" role="alert">
            Não foi possível carregar suas listas: {loadError}
          </div>
          <button type="button" className="btn-secondary" onClick={reload}>
            Tentar novamente
          </button>
        </div>
      ) : (
        <div className="lists-content">
          <aside className="lists-sidebar" aria-label="Suas listas">
            <button type="button" className="btn-primary lists-new" onClick={startCreating} disabled={saving}>
              + Nova lista
            </button>
            {lists.length === 0 ? (
              <p className="lists-empty">Você ainda não tem listas.</p>
            ) : (
              <ul className="lists-nav">
                {lists.map((list) => (
                  <li key={list.id}>
                    <button
                      type="button"
                      className={[
                        'lists-nav-item',
                        list.isWatchlist && 'is-watchlist',
                        list.id === selectedId && mode !== 'create' && 'active',
                      ]
                        .filter(Boolean)
                        .join(' ')}
                      onClick={() => selectList(list.id)}
                    >
                      <span className="lists-nav-title">{list.title}</span>
                      <span className="lists-nav-meta">
                        {list.isWatchlist && <span className="lists-nav-pin">Fixa</span>}
                        {list.items.length} {list.items.length === 1 ? 'filme' : 'filmes'} ·{' '}
                        {list.isPublic ? 'Pública' : 'Privada'}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </aside>

          <div className="profile-form-wrap">
            {error && <div className="alert alert-error" role="alert">{error}</div>}
            {notice && <div className="alert alert-success" role="status">{notice}</div>}

            {mode === 'create' || mode === 'edit' ? (
              <form onSubmit={handleSubmit}>
                <h2 className="lists-title">
                  {mode === 'create' ? 'Nova lista' : editingWatchlist ? 'Editar watchlist' : 'Editar lista'}
                </h2>

                <div className="field">
                  <label htmlFor="list-title">Título</label>
                  <input
                    id="list-title"
                    name="title"
                    maxLength={100}
                    value={form.title}
                    onChange={(event) => setForm({ ...form, title: event.target.value })}
                    readOnly={editingWatchlist}
                    aria-describedby={editingWatchlist ? 'list-title-hint' : undefined}
                    required
                  />
                  {editingWatchlist && (
                    <span id="list-title-hint" className="field-hint">
                      O nome da watchlist não pode ser alterado. Descrição e visibilidade, sim.
                    </span>
                  )}
                </div>

                <div className="field">
                  <label htmlFor="list-description">Descrição</label>
                  <textarea
                    id="list-description"
                    name="description"
                    rows={3}
                    maxLength={500}
                    value={form.description}
                    onChange={(event) => setForm({ ...form, description: event.target.value })}
                  />
                </div>

                <label className="checkbox-field">
                  <input
                    type="checkbox"
                    checked={form.isPublic}
                    onChange={(event) => setForm({ ...form, isPublic: event.target.checked })}
                  />
                  Lista pública (outros usuários podem ver)
                </label>

                <button type="submit" className="btn-primary" disabled={saving}>
                  {saving ? 'Salvando...' : mode === 'create' ? 'Criar lista' : 'Salvar alterações'}
                </button>
                {(mode === 'edit' || lists.length > 0) && (
                  <button type="button" className="btn-secondary" onClick={cancelForm} disabled={saving}>
                    Cancelar
                  </button>
                )}
              </form>
            ) : selectedList && (
              <>
                <div className="list-header">
                  <div>
                    <h2 className="lists-title">{selectedList.title}</h2>
                    <span className={selectedList.isPublic ? 'badge badge-public' : 'badge'}>
                      {selectedList.isPublic ? 'Pública' : 'Privada'}
                    </span>
                    {selectedList.isWatchlist && <span className="badge badge-fixed">Lista fixa</span>}
                  </div>
                  <div className="list-actions">
                    <button type="button" className="btn-secondary" onClick={startEditing} disabled={saving}>
                      Editar
                    </button>
                    {!selectedList.isWatchlist && (
                      <button type="button" className="btn-danger" onClick={handleDelete} disabled={saving}>
                        Excluir
                      </button>
                    )}
                  </div>
                </div>
                {selectedList.isWatchlist && (
                  <p className="field-hint list-watchlist-hint">
                    Filmes guardados para assistir depois. O botão “Adicionar à watchlist” da ficha do filme
                    coloca o filme aqui. Esta lista não pode ser renomeada nem excluída.
                  </p>
                )}
                {selectedList.description && <p className="list-description">{selectedList.description}</p>}

                <div className="field movie-search">
                  <label htmlFor="movie-search">Adicionar filme</label>
                  <input
                    id="movie-search"
                    type="search"
                    placeholder="Busque pelo título..."
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                  />
                  {search.trim() && (
                    <ul className="movie-results">
                      {searching && <li className="movie-results-empty">Buscando...</li>}
                      {!searching && results.length === 0 && (
                        <li className="movie-results-empty">Nenhum filme encontrado.</li>
                      )}
                      {!searching && results.map((movie) => (
                        <li key={movie.id}>
                          <span>
                            {movie.title}
                            {movie.releaseDate && <small> ({movie.releaseDate.slice(0, 4)})</small>}
                          </span>
                          <button
                            type="button"
                            className="link"
                            onClick={() => handleAdd(movie)}
                            disabled={saving || listMovieIds.has(movie.id)}
                          >
                            {listMovieIds.has(movie.id) ? 'Na lista' : 'Adicionar'}
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>

                {selectedList.items.length === 0 ? (
                  <p className="lists-empty">
                    {selectedList.isWatchlist
                      ? 'Sua watchlist está vazia. Busque um filme acima ou use “Adicionar à watchlist” na ficha.'
                      : 'Esta lista ainda não tem filmes.'}
                  </p>
                ) : (
                  <ol className="list-items">
                    {selectedList.items.map((item, index) => (
                      <li key={item.movieId}>
                        <span className="list-item-position">{index + 1}</span>
                        <span className="list-item-title">
                          {onOpenMovie ? (
                            <button
                              type="button"
                              className="link list-item-open"
                              onClick={() => onOpenMovie(item.movieId)}
                              title="Abrir a ficha do filme"
                            >
                              {item.movieTitle}
                            </button>
                          ) : (
                            item.movieTitle
                          )}
                        </span>
                        <div className="list-item-actions">
                          <button
                            type="button"
                            className="icon-button"
                            onClick={() => handleMove(index, -1)}
                            disabled={saving || index === 0}
                            aria-label={`Mover ${item.movieTitle} para cima`}
                          >
                            ↑
                          </button>
                          <button
                            type="button"
                            className="icon-button"
                            onClick={() => handleMove(index, 1)}
                            disabled={saving || index === selectedList.items.length - 1}
                            aria-label={`Mover ${item.movieTitle} para baixo`}
                          >
                            ↓
                          </button>
                          <button
                            type="button"
                            className="icon-button icon-button-danger"
                            onClick={() => handleRemove(item)}
                            disabled={saving}
                            aria-label={`Remover ${item.movieTitle}`}
                          >
                            ×
                          </button>
                        </div>
                      </li>
                    ))}
                  </ol>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </section>
  )
}
