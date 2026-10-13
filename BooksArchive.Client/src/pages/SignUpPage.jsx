import { useState } from 'react'
import TextField from '../components/TextField.jsx'
import { signIn, signUp } from '../services/api.js'

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

// Mesmas regras do CreateUserRequestDto no back-end
function validate(form) {
  const errs = {}
  if (!form.name.trim()) errs.name = 'Nome de usuário é obrigatório'
  if (!form.email.trim()) errs.email = 'O email é obrigatório'
  else if (!EMAIL_REGEX.test(form.email.trim())) errs.email = 'Insira um email de formato válido'
  if (!form.password) errs.password = 'A senha é obrigatória'
  else if (form.password.length < 6) errs.password = 'A senha deve ter pelo menos 6 caracteres'
  if (form.confirmPassword !== form.password) errs.confirmPassword = 'As senhas não coincidem'
  return errs
}

export default function SignUpPage({ onLoggedIn, onSignedUp, onGoToLogin }) {
  const [form, setForm] = useState({ name: '', email: '', password: '', confirmPassword: '' })
  const [fieldErrors, setFieldErrors] = useState({})
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  function handleChange(e) {
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))
    setFieldErrors((errs) => ({ ...errs, [name]: undefined }))
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')

    const errs = validate(form)
    setFieldErrors(errs)
    if (Object.keys(errs).length) return

    const payload = { name: form.name.trim(), email: form.email.trim(), password: form.password }

    setLoading(true)
    try {
      await signUp(payload)
    } catch (err) {
      const apiErrs = err.fieldErrors ?? {}
      setFieldErrors(apiErrs)
      // Erros de campo já aparecem sob o input; o alerta fica para erros gerais
      setError(Object.keys(apiErrs).length ? '' : err.message)
      setLoading(false)
      return
    }

    // Conta criada: tenta entrar automaticamente
    try {
      const token = await signIn(payload)
      onLoggedIn(token)
    } catch {
      onSignedUp('Conta criada com sucesso! Faça login para continuar.')
    }
  }

  return (
    <div className="card">
      <h1>Criar conta</h1>
      <p className="subtitle">Comece a montar seu acervo de livros</p>

      {error && <div className="alert alert-error">{error}</div>}

      <form onSubmit={handleSubmit} noValidate>
        <TextField
          label="Nome de usuário"
          name="name"
          autoComplete="username"
          value={form.name}
          onChange={handleChange}
          error={fieldErrors.name}
          autoFocus
        />
        <TextField
          label="Email"
          name="email"
          type="email"
          autoComplete="email"
          value={form.email}
          onChange={handleChange}
          error={fieldErrors.email}
        />
        <TextField
          label="Senha"
          name="password"
          type="password"
          autoComplete="new-password"
          value={form.password}
          onChange={handleChange}
          error={fieldErrors.password}
        />
        <TextField
          label="Confirmar senha"
          name="confirmPassword"
          type="password"
          autoComplete="new-password"
          value={form.confirmPassword}
          onChange={handleChange}
          error={fieldErrors.confirmPassword}
        />
        <button type="submit" className="btn-primary" disabled={loading}>
          {loading ? 'Criando conta…' : 'Criar conta'}
        </button>
      </form>

      <p className="switch">
        Já tem conta?{' '}
        <button type="button" className="link" onClick={onGoToLogin}>
          Entrar
        </button>
      </p>
    </div>
  )
}
