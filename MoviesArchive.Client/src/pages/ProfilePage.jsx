import { useEffect, useState } from 'react'
import DeleteAccountDialog from '../components/DeleteAccountDialog.jsx'
import { formatDate, todayIso } from '../utils/dates.js'
import { getMyProfile, getPhotoSrc, removeMyProfilePhoto, updateMyProfile, uploadMyProfilePhoto } from '../services/api.js'

export default function ProfilePage({ token, onBack, onProfileUpdated, onAccountDeleted }) {
  const [profile, setProfile] = useState(null)
  const [name, setName] = useState('')
  const [birthDate, setBirthDate] = useState('') // só editável enquanto a conta não tiver data
  const [selectedPhoto, setSelectedPhoto] = useState(null)
  const [removePhoto, setRemovePhoto] = useState(false)
  const [previewUrl, setPreviewUrl] = useState(null)
  const [imageFailed, setImageFailed] = useState(false)
  const [isEditing, setIsEditing] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)

  useEffect(() => {
    let active = true

    getMyProfile(token)
      .then((data) => {
        if (!active) return
        setProfile(data)
        setName(data.name)
        setBirthDate(data.birthDate ?? '')
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

  useEffect(() => {
    if (!selectedPhoto) {
      setPreviewUrl(null)
      return
    }

    const objectUrl = URL.createObjectURL(selectedPhoto)
    setPreviewUrl(objectUrl)
    return () => URL.revokeObjectURL(objectUrl)
  }, [selectedPhoto])

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setNotice('')

    const trimmedName = name.trim()
    if (!trimmedName) {
      setError('Nome de usuário é obrigatório.')
      return
    }

    const newBirthDate = !profile?.birthDate && birthDate ? birthDate : undefined
    if (newBirthDate && (newBirthDate > todayIso() || newBirthDate < '1900-01-01')) {
      setError('Informe uma data de nascimento válida.')
      return
    }

    setSaving(true)
    try {
      await updateMyProfile(token, { name: trimmedName, birthDate: newBirthDate })
      if (selectedPhoto) {
        await uploadMyProfilePhoto(token, selectedPhoto)
      } else if (removePhoto) {
        await removeMyProfilePhoto(token)
      }
      const updated = await getMyProfile(token)
      setProfile(updated)
      setName(updated.name)
      setSelectedPhoto(null)
      setRemovePhoto(false)
      setIsEditing(false)
      onProfileUpdated(updated)
      setNotice('Perfil atualizado.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setSaving(false)
    }
  }

  function cancelEditing() {
    setName(profile?.name ?? '')
    setBirthDate(profile?.birthDate ?? '')
    setSelectedPhoto(null)
    setRemovePhoto(false)
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
          <h1>Seu perfil</h1>
        </div>
      </div>

      <div className="profile-content">
        <aside className="profile-aside" aria-label="Prévia da foto de perfil">
          {(previewUrl || (!removePhoto && profile?.photoUrl)) && !imageFailed ? (
            <img
              className="profile-avatar"
              src={previewUrl || getPhotoSrc(profile.photoUrl)}
              alt="Prévia da foto de perfil"
              onError={() => setImageFailed(true)}
            />
          ) : (
            <div className="profile-avatar profile-avatar-empty" aria-hidden="true">
              {name.trim().charAt(0).toUpperCase() || '?'}
            </div>
          )}
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
                    <label htmlFor="profile-birthdate">Data de nascimento</label>
                    {profile?.birthDate ? (
                      <input id="profile-birthdate" value={formatDate(profile.birthDate)} readOnly />
                    ) : (
                      <input
                        id="profile-birthdate"
                        type="date"
                        min="1900-01-01"
                        max={todayIso()}
                        value={birthDate}
                        onChange={(event) => setBirthDate(event.target.value)}
                      />
                    )}
                    <span className="field-hint">
                      {profile?.birthDate
                        ? 'A data de nascimento não pode ser alterada.'
                        : 'Necessária para assinar um plano. Depois de salva, não pode ser alterada.'}
                    </span>
                  </div>

                  <div className="field">
                    <label htmlFor="profile-photo">
                      Foto de perfil (JPEG, PNG ou WebP. Até 5 MB)
                    </label>
                    <input
                      id="profile-photo"
                      name="photo"
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      onChange={(event) => {
                        setSelectedPhoto(event.target.files?.[0] ?? null)
                        setRemovePhoto(false)
                        setImageFailed(false)
                      }}
                    />
                  </div>

                  {profile?.photoUrl && !removePhoto && (
                    <button
                      type="button"
                      className="btn-secondary"
                      onClick={() => {
                        setSelectedPhoto(null)
                        setRemovePhoto(true)
                        setImageFailed(false)
                      }}
                      disabled={saving}
                    >
                      Remover foto atual
                    </button>
                  )}

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
                    <dt>Data de nascimento</dt>
                    <dd>{profile?.birthDate ? formatDate(profile.birthDate) : 'Não informada'}</dd>
                    <dt>Foto</dt>
                    <dd>{profile?.photoUrl ? 'Configurada' : 'Não definida'}</dd>
                  </dl>
                  <button type="button" className="btn-primary" onClick={() => setIsEditing(true)}>
                    Editar perfil
                  </button>

                  <fieldset className="danger-zone">
                    <legend>Zona de perigo</legend>
                    <p>Ao excluir a conta, você escolhe o que acontece com o seu conteúdo. Não dá para desfazer.</p>
                    <button type="button" className="btn-secondary" onClick={() => setDeleteOpen(true)}>
                      Excluir conta…
                    </button>
                  </fieldset>
                </>
              )}
            </>
          )}

          <button type="button" className="btn-secondary profile-back-secondary" onClick={onBack}>
            Voltar ao início
          </button>
        </div>
      </div>

      {deleteOpen && (
        <DeleteAccountDialog
          token={token}
          onClose={() => setDeleteOpen(false)}
          onDeleted={onAccountDeleted}
        />
      )}
    </section>
  )
}