import { getPhotoSrc } from '../services/api.js'

export default function HomePage({ user, onLogout }) {
  const expires = user.expiresAt?.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })

  return (
    <div className="card">
      <h1>Olá, {user.name}!</h1>
      <p className="subtitle">Você está conectado ao Movies Archive.</p>

      {user.photoUrl && (
        <img className="home-avatar" src={getPhotoSrc(user.photoUrl)} alt="Foto de perfil" />
      )}

      <dl className="user-info">
        <dt>Usuário</dt>
        <dd>{user.name}</dd>
        <dt>Email</dt>
        <dd>{user.email}</dd>
        {expires && (
          <>
            <dt>Sessão expira às</dt>
            <dd>{expires}</dd>
          </>
        )}
      </dl>

      <button type="button" className="btn-secondary" onClick={onLogout}>
        Sair
      </button>
    </div>
  )
}
