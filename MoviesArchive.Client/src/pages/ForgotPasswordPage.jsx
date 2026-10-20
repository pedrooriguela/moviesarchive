import { useState } from 'react'
import TextField from '../components/TextField.jsx'
import { requestPasswordReset } from '../services/api.js'

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export default function ForgotPasswordPage({ onGoToLogin }) {
  const [email, setEmail] = useState('')
  const [fieldError, setFieldError] = useState('')
  const [error, setError] = useState('')
  const [sent, setSent] = useState(false)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')

    const value = email.trim()
    if (!value) return setFieldError('O email é obrigatório')
    if (!EMAIL_REGEX.test(value)) return setFieldError('Insira um email de formato válido')

    setLoading(true)
    try {
      await requestPasswordReset(value)
      setSent(true)
    } catch (err) {
      const apiError = err.fieldErrors?.email
      setFieldError(apiError ?? '')
      setError(apiError ? '' : err.message)
    } finally {
      setLoading(false)
    }
  }

  if (sent) {
    return (
      <div className="card">
        <h1>Confira seu e-mail</h1>
        <div className="alert alert-success">
          Se <strong>{email.trim()}</strong> estiver cadastrado, enviamos um link para criar uma
          nova senha. Ele vale por 15 minutos.
        </div>
        <p className="subtitle">Não chegou? Olhe a caixa de spam ou peça de novo.</p>
        <button type="button" className="btn-secondary" onClick={() => setSent(false)}>
          Enviar de novo
        </button>
        <p className="switch">
          <button type="button" className="link" onClick={onGoToLogin}>
            Voltar para o login
          </button>
        </p>
      </div>
    )
  }

  return (
    <div className="card">
      <h1>Esqueci minha senha</h1>
      <p className="subtitle">Informe o e-mail da sua conta para receber um link de redefinição.</p>

      {error && <div className="alert alert-error">{error}</div>}

      <form onSubmit={handleSubmit} noValidate>
        <TextField
          label="Email"
          name="email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => {
            setEmail(e.target.value)
            setFieldError('')
          }}
          error={fieldError}
          autoFocus
        />
        <button type="submit" className="btn-primary" disabled={loading}>
          {loading ? 'Enviando…' : 'Enviar link'}
        </button>
      </form>

      <p className="switch">
        Lembrou a senha?{' '}
        <button type="button" className="link" onClick={onGoToLogin}>
          Entrar
        </button>
      </p>
    </div>
  )
}
