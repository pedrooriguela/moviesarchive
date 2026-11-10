import { useEffect, useState } from 'react'
import Dialog from './Dialog.jsx'
import { addListItem, createList, getMyLists, removeListItem } from '../services/api.js'

// Janela da ficha do filme: marca e desmarca o filme nas listas da pessoa e cria lista nova.
// Cada caixa salva na hora; cada linha tem o próprio "salvando" e o próprio erro.
export default function AddToListDialog({ token, movie, onClose }) {
  const [lists, setLists] = useState({ status: 'loading', items: [], error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [saving, setSaving] = useState({}) // listId -> true enquanto salva
  const [rowErrors, setRowErrors] = useState({}) // listId -> mensagem
  const [newTitle, setNewTitle] = useState('')
  const [newIsPublic, setNewIsPublic] = useState(false)
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')
  const [flash, setFlash] = useState('')

  useEffect(() => {
    let active = true
    getMyLists(token)
      .then((items) => {
        if (active) setLists({ status: 'ready', items, error: '' })
      })
      .catch((requestError) => {
        if (active) setLists({ status: 'error', items: [], error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [token, reloadKey])

  const busy = creating || Object.values(saving).some(Boolean)

  function contains(list) {
    return list.items.some((item) => item.movieId === movie.id)
  }

  function replaceList(updated) {
    setLists((current) => ({
      ...current,
      items: current.items.map((list) => (list.id === updated.id ? updated : list)),
    }))
  }

  // Sem a resposta da API (ex.: o filme já estava lá), ajusta só a presença do filme na lista
  function markLocally(list, present) {
    replaceList({
      ...list,
      items: present
        ? [...list.items, { movieId: movie.id, movieTitle: movie.title, position: list.items.length }]
        : list.items.filter((item) => item.movieId !== movie.id),
    })
  }

  async function toggle(list) {
    const adding = !contains(list)
    setSaving((current) => ({ ...current, [list.id]: true }))
    setRowErrors((current) => ({ ...current, [list.id]: '' }))
    setFlash('')
    try {
      const updated = adding
        ? await addListItem(token, list.id, movie.id)
        : await removeListItem(token, list.id, movie.id)
      replaceList(updated)
    } catch (requestError) {
      // Já estava lá (409) ou já tinha saído (404 do item): o resultado desejado já existe
      if (adding && requestError.status === 409) markLocally(list, true)
      else if (!adding && requestError.status === 404) markLocally(list, false)
      else setRowErrors((current) => ({ ...current, [list.id]: requestError.message }))
    } finally {
      setSaving((current) => ({ ...current, [list.id]: false }))
    }
  }

  async function handleCreate(event) {
    event.preventDefault()
    const title = newTitle.trim()
    if (!title) {
      setCreateError('Dê um nome para a lista.')
      return
    }

    setCreating(true)
    setCreateError('')
    setFlash('')
    let created = null
    try {
      created = await createList(token, { title, description: '', isPublic: newIsPublic })
      const withMovie = await addListItem(token, created.id, movie.id)
      setLists((current) => ({ ...current, items: [withMovie, ...current.items] }))
      setFlash(`Lista “${title}” criada com ${movie.title}.`)
      setNewTitle('')
      setNewIsPublic(false)
    } catch (requestError) {
      if (created) {
        // A lista foi criada, mas o filme não entrou: mostra a lista para tentar de novo pela caixa
        setLists((current) => ({ ...current, items: [created, ...current.items] }))
        setRowErrors((current) => ({ ...current, [created.id]: requestError.message }))
        setNewTitle('')
      } else {
        setCreateError(requestError.fieldErrors?.title ?? requestError.message)
      }
    } finally {
      setCreating(false)
    }
  }

  function renderLists() {
    if (lists.status === 'loading') {
      return (
        <p className="field-hint" role="status">
          Carregando suas listas...
        </p>
      )
    }

    if (lists.status === 'error') {
      return (
        <div className="results-state">
          <div className="alert alert-error" role="alert">
            {lists.error}
          </div>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => {
              setLists({ status: 'loading', items: [], error: '' })
              setReloadKey((key) => key + 1)
            }}
          >
            Tentar novamente
          </button>
        </div>
      )
    }

    if (lists.items.length === 0) {
      return <p className="list-picker-empty">Você ainda não tem listas. Crie a primeira abaixo.</p>
    }

    return (
      <ul className="list-picker">
        {lists.items.map((list) => {
          const count = list.items.length
          return (
            <li key={list.id}>
              <label className="list-pick">
                <input
                  type="checkbox"
                  checked={contains(list)}
                  onChange={() => toggle(list)}
                  disabled={Boolean(saving[list.id])}
                />
                <span className="list-pick-title">{list.title}</span>
                <span className="list-pick-meta">
                  {saving[list.id] ? 'salvando…' : `${count} ${count === 1 ? 'filme' : 'filmes'}`}
                </span>
                <span className={list.isPublic ? 'badge badge-public' : 'badge'}>
                  {list.isPublic ? 'Pública' : 'Privada'}
                </span>
              </label>
              {rowErrors[list.id] && (
                <p className="field-error list-pick-error" role="alert">
                  {rowErrors[list.id]}
                </p>
              )}
            </li>
          )
        })}
      </ul>
    )
  }

  return (
    <Dialog title="Adicionar à lista" onClose={() => !busy && onClose()}>
      <p className="dialog-lead">
        Marque as listas em que <strong>{movie.title}</strong> deve aparecer. Cada mudança é salva na hora.
      </p>

      {flash && (
        <div className="alert alert-success" role="status">
          {flash}
        </div>
      )}

      {renderLists()}

      <form className="list-create" onSubmit={handleCreate} noValidate>
        <fieldset className="group-box">
          <legend>Criar nova lista</legend>
          <div className="field">
            <label htmlFor="nova-lista-titulo">Nome</label>
            <input
              id="nova-lista-titulo"
              value={newTitle}
              maxLength={100}
              onChange={(event) => {
                setNewTitle(event.target.value)
                setCreateError('')
              }}
              aria-invalid={Boolean(createError)}
              aria-describedby={createError ? 'nova-lista-erro' : undefined}
            />
            {createError && (
              <span id="nova-lista-erro" className="field-error">
                {createError}
              </span>
            )}
          </div>
          <label className="checkbox-field">
            <input type="checkbox" checked={newIsPublic} onChange={(event) => setNewIsPublic(event.target.checked)} />
            Lista pública (outros usuários podem ver)
          </label>
          <button type="submit" className="btn-secondary" disabled={creating}>
            {creating ? 'Criando…' : 'Criar e adicionar o filme'}
          </button>
        </fieldset>
      </form>

      <div className="dialog-actions">
        <button type="button" className="btn-primary dialog-submit" onClick={onClose} disabled={busy}>
          Concluir
        </button>
      </div>
    </Dialog>
  )
}
