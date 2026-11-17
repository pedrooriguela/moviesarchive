import { useState } from 'react'
import { AGE_RATINGS } from '../../constants/ageRatings.js'
import { GENRES } from '../../constants/genres.js'
import { LANGUAGE_OPTIONS } from '../../constants/languages.js'
import { saveAdminMovie } from '../../services/admin.js'

const EMPTY = {
  title: '',
  releaseDate: '',
  director: '',
  language: '',
  runtimeMinutes: '',
  ageRating: '',
  genres: [],
  description: '',
  posterUrl: '',
  videoUrl: '',
}

function toForm(movie) {
  if (!movie) return EMPTY
  return {
    title: movie.title ?? '',
    releaseDate: movie.releaseDate ?? '',
    director: movie.director ?? '',
    language: movie.language ?? '',
    runtimeMinutes: movie.runtimeMinutes ?? '',
    ageRating: String(movie.ageRating ?? ''),
    genres: movie.genres ?? [],
    description: movie.description ?? '',
    posterUrl: movie.posterUrl ?? '',
    videoUrl: movie.videoUrl ?? '',
  }
}

// A API devolve os erros de validação com o nome do campo ("Title" ou "title"): vira o nome do formulário
function normalizeErrors(fieldErrors = {}) {
  return Object.fromEntries(
    Object.entries(fieldErrors).map(([key, message]) => [key.charAt(0).toLowerCase() + key.slice(1), message]),
  )
}

// Cadastro (movie nulo) e edição de filme no painel do administrador
export default function MovieForm({ token, movie, onSaved, onCancel }) {
  const [form, setForm] = useState(() => toForm(movie))
  const [errors, setErrors] = useState({})
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  function update(patch) {
    setForm((current) => ({ ...current, ...patch }))
    setErrors((current) => {
      const next = { ...current }
      for (const key of Object.keys(patch)) delete next[key]
      return next
    })
  }

  function toggleGenre(value) {
    update({
      genres: form.genres.includes(value) ? form.genres.filter((genre) => genre !== value) : [...form.genres, value],
    })
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')

    const found = {}
    if (!form.title.trim()) found.title = 'O título é obrigatório.'
    if (form.genres.length === 0) found.genres = 'Escolha pelo menos um gênero.'
    if (form.ageRating === '') found.ageRating = 'Escolha a classificação indicativa.'
    if (form.runtimeMinutes !== '' && Number(form.runtimeMinutes) <= 0) found.runtimeMinutes = 'Informe a duração em minutos.'
    if (Object.keys(found).length > 0) {
      setErrors(found)
      return
    }

    setSaving(true)
    try {
      const saved = await saveAdminMovie(token, movie?.id, {
        title: form.title.trim(),
        releaseDate: form.releaseDate || null,
        director: form.director.trim() || null,
        language: form.language.trim() || null,
        runtimeMinutes: form.runtimeMinutes === '' ? null : Number(form.runtimeMinutes),
        ageRating: Number(form.ageRating),
        genres: form.genres,
        description: form.description.trim() || null,
        posterUrl: form.posterUrl.trim() || null,
        videoUrl: form.videoUrl.trim() || null,
      })
      onSaved(saved, movie ? 'updated' : 'created')
    } catch (requestError) {
      const fieldErrors = normalizeErrors(requestError.fieldErrors)
      setErrors(fieldErrors)
      if (Object.keys(fieldErrors).length === 0) setError(requestError.message)
    } finally {
      setSaving(false)
    }
  }

  function fieldError(name) {
    return errors[name] ? (
      <span id={`filme-${name}-erro`} className="field-error">
        {errors[name]}
      </span>
    ) : null
  }

  function describedBy(name, hint) {
    return [errors[name] && `filme-${name}-erro`, hint].filter(Boolean).join(' ') || undefined
  }

  return (
    <form className="admin-movie-form" onSubmit={handleSubmit} noValidate>
      <h2 className="lists-title">{movie ? `Editar “${movie.title}”` : 'Cadastrar filme'}</h2>
      {movie?.isRemoved && (
        <div className="alert alert-info" role="status">
          Este filme está removido do catálogo. As alterações valem quando ele for restaurado.
        </div>
      )}
      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      <div className="admin-form-grid">
        <div className="field admin-field-wide">
          <label htmlFor="filme-title">Título *</label>
          <input
            id="filme-title"
            maxLength={200}
            value={form.title}
            onChange={(event) => update({ title: event.target.value })}
            aria-invalid={Boolean(errors.title)}
            aria-describedby={describedBy('title')}
            autoFocus
          />
          {fieldError('title')}
        </div>

        <div className="field">
          <label htmlFor="filme-releaseDate">Data de lançamento</label>
          <input
            id="filme-releaseDate"
            type="date"
            min="1880-01-01"
            max="2100-12-31"
            value={form.releaseDate}
            onChange={(event) => update({ releaseDate: event.target.value })}
            aria-invalid={Boolean(errors.releaseDate)}
            aria-describedby={describedBy('releaseDate')}
          />
          {fieldError('releaseDate')}
        </div>

        <div className="field">
          <label htmlFor="filme-director">Direção</label>
          <input
            id="filme-director"
            maxLength={200}
            value={form.director}
            onChange={(event) => update({ director: event.target.value })}
            aria-invalid={Boolean(errors.director)}
            aria-describedby={describedBy('director')}
          />
          {fieldError('director')}
        </div>

        <div className="field">
          <label htmlFor="filme-language">Idioma</label>
          <input
            id="filme-language"
            list="filme-idiomas"
            maxLength={50}
            value={form.language}
            onChange={(event) => update({ language: event.target.value })}
            aria-invalid={Boolean(errors.language)}
            aria-describedby={describedBy('language', 'filme-language-dica')}
          />
          <datalist id="filme-idiomas">
            {LANGUAGE_OPTIONS.map((language) => (
              <option key={language} value={language} />
            ))}
          </datalist>
          <span id="filme-language-dica" className="field-hint">
            Use o nome por extenso (ex.: Inglês) para o filtro da busca encontrar.
          </span>
          {fieldError('language')}
        </div>

        <div className="field">
          <label htmlFor="filme-runtimeMinutes">Duração (minutos)</label>
          <input
            id="filme-runtimeMinutes"
            type="number"
            inputMode="numeric"
            min="1"
            max="1000"
            value={form.runtimeMinutes}
            onChange={(event) => update({ runtimeMinutes: event.target.value })}
            aria-invalid={Boolean(errors.runtimeMinutes)}
            aria-describedby={describedBy('runtimeMinutes')}
          />
          {fieldError('runtimeMinutes')}
        </div>

        <div className="field">
          <label htmlFor="filme-ageRating">Classificação indicativa *</label>
          <select
            id="filme-ageRating"
            value={form.ageRating}
            onChange={(event) => update({ ageRating: event.target.value })}
            aria-invalid={Boolean(errors.ageRating)}
            aria-describedby={describedBy('ageRating')}
          >
            <option value="">Escolha…</option>
            {AGE_RATINGS.map((rating) => (
              <option key={rating.value} value={rating.value}>
                {rating.label}
              </option>
            ))}
          </select>
          {fieldError('ageRating')}
        </div>
      </div>

      <fieldset className="genre-filter admin-genres" aria-invalid={Boolean(errors.genres)}>
        <legend>Gêneros *</legend>
        <div className="genre-options">
          {GENRES.map((genre) => (
            <label key={genre.value} className="genre-option">
              <input type="checkbox" checked={form.genres.includes(genre.value)} onChange={() => toggleGenre(genre.value)} />
              {genre.label}
            </label>
          ))}
        </div>
        {fieldError('genres')}
      </fieldset>

      <div className="field">
        <label htmlFor="filme-description">Sinopse</label>
        <textarea
          id="filme-description"
          rows={4}
          maxLength={4000}
          value={form.description}
          onChange={(event) => update({ description: event.target.value })}
          aria-invalid={Boolean(errors.description)}
          aria-describedby={describedBy('description')}
        />
        {fieldError('description')}
      </div>

      <div className="field">
        <label htmlFor="filme-posterUrl">Endereço do pôster (imagem)</label>
        <input
          id="filme-posterUrl"
          type="url"
          placeholder="https://..."
          value={form.posterUrl}
          onChange={(event) => update({ posterUrl: event.target.value })}
          aria-invalid={Boolean(errors.posterUrl)}
          aria-describedby={describedBy('posterUrl', 'filme-posterUrl-dica')}
        />
        <span id="filme-posterUrl-dica" className="field-hint">
          Opcional. Sem pôster, a ficha mostra uma arte gerada com o título.
        </span>
        {fieldError('posterUrl')}
      </div>

      <div className="field">
        <label htmlFor="filme-videoUrl">Endereço do vídeo (MP4 ou WebM)</label>
        <input
          id="filme-videoUrl"
          type="url"
          placeholder="https://archive.org/download/..."
          value={form.videoUrl}
          onChange={(event) => update({ videoUrl: event.target.value })}
          aria-invalid={Boolean(errors.videoUrl)}
          aria-describedby={describedBy('videoUrl', 'filme-videoUrl-dica')}
        />
        <span id="filme-videoUrl-dica" className="field-hint">
          Sem vídeo, o filme aparece no catálogo com o botão “Vídeo indisponível”.
        </span>
        {fieldError('videoUrl')}
      </div>

      <div className="admin-form-actions">
        <button type="submit" className="btn-primary" disabled={saving}>
          {saving ? 'Salvando...' : movie ? 'Salvar alterações' : 'Cadastrar filme'}
        </button>
        <button type="button" className="btn-secondary" onClick={onCancel} disabled={saving}>
          Cancelar
        </button>
      </div>
    </form>
  )
}
