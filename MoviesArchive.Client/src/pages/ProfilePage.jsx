import { useEffect, useState } from 'react'
import { getMyProfile, updateMyProfile } from '../services/api.js'

export default function ProfilePage({ token, onBack, onProfileUpdated }) {
  const [profile, setProfile] = useState(null)
  const [name, setName] = useState('')
  const [photoUrl, setPhotoUrl] = useState('')
  const [imageFailed, setImageFailed] = useState(false)
  const [isEditing, setIsEditing] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true

    getMyProfile(token)
      .then((data) => {
        if (!active) return
        setProfile(data)
        setName(data.name)
        setPhotoUrl(data.photoUrl ?? '')
        onProfileUpdated(data)
      })
      .catch((requestError) => {
        if (active) setError(requestError.message)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [token, onProfileUpdated])

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setNotice('')

    const trimmedName = name.trim()
    if (!trimmedName) {
      setError('Nome de usuário é obrigatório.')
      return
    }

    setSaving(true)
    try {
      const updated = await updateMyProfile(token, {
        name: trimmedName,
        photoUrl: photoUrl.trim(),
      })
      setProfile(updated)
      setName(updated.name)
      setPhotoUrl(updated.photoUrl ?? '')
      setIsEditing(false)
      onProfileUpdated(updated)
      setNotice('Perfil atualizado.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setSaving(false)
    }
  }

  const previewUrl = photoUrl.trim() || profile?.photoUrl

  function cancelEditing() {
    setName(profile?.name ?? '')
    setPhotoUrl(profile?.photoUrl ?? '')
    setImageFailed(false)
    setError('')
    setIsEditing(false)
  }

  return (
    <section className="profile-layout">
      <div className="profile-heading">
        <button type="button" className="back-button" onClick={onBack} aria-label="Voltar ao início">
          ← Voltar
        </button>
        <div>
          <p className="eyebrow">Conta</p>
          <h1>Seu perfil</h1>
        </div>
      </div>

      <div className="profile-content">
        <aside className="profile-aside" aria-label="Prévia da foto de perfil">
          {previewUrl && !imageFailed ? (
            <img
              className="profile-avatar"
              src={previewUrl}
              alt="Prévia da foto de perfil"
              onError={() => setImageFailed(true)}
            />
          ) : (
            <div className="profile-avatar profile-avatar-empty" aria-hidden="true">
              {name.trim().charAt(0).toUpperCase() || '?'}
            </div>
          )}
          <p className="avatar-caption">A foto precisa estar hospedada publicamente.</p>
        </aside>

        <div className="profile-form-wrap">
          {loading ? (
            <p role="status">Carregando perfil...</p>
          ) : (
            <>
              {error && <div className="alert alert-error" role="alert">{error}</div>}
              {notice && <div className="alert alert-success" role="status">{notice}</div>}

              {isEditing ? (
                <form onSubmit={handleSubmit}>
                  <div className="field">
                    <label htmlFor="profile-name">Nome de usuário</label>
                    <input
                      id="profile-name"
                      name="name"
                      autoComplete="username"
                      maxLength={50}
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                      required
                    />
                  </div>

                  <div className="field">
                    <label htmlFor="profile-email">Email</label>
                    <input id="profile-email" value={profile?.email ?? ''} readOnly />
                  </div>

                  <div className="field">
                    <label htmlFor="profile-photo">URL da foto</label>
                    <input
                      id="profile-photo"
                      name="photoUrl"
                      type="url"
                      inputMode="url"
                      placeholder="https://exemplo.com/minha-foto.jpg"
                      value={photoUrl}
                      onChange={(event) => {
                        setPhotoUrl(event.target.value)
                        setImageFailed(false)
                      }}
                    />
                  </div>

                  <button type="submit" className="btn-primary" disabled={saving}>
                    {saving ? 'Salvando...' : 'Salvar alterações'}
                  </button>
                  <button type="button" className="btn-secondary" onClick={cancelEditing} disabled={saving}>
                    Cancelar
                  </button>
                </form>
              ) : (
                <>
                  <dl className="profile-details">
                    <dt>Nome de usuário</dt>
                    <dd>{profile?.name}</dd>
                    <dt>Email</dt>
                    <dd>{profile?.email}</dd>
                    <dt>Foto</dt>
                    <dd>{profile?.photoUrl ? 'Configurada' : 'Não definida'}</dd>
                  </dl>
                  <button type="button" className="btn-primary" onClick={() => setIsEditing(true)}>
                    Editar perfil
                  </button>
                </>
              )}
            </>
          )}

          <button type="button" className="btn-secondary profile-back-secondary" onClick={onBack}>
            Voltar ao início
          </button>
        </div>
      </div>
    </section>
  )
}