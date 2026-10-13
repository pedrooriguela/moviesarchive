import { useState } from 'react'
import TextField from '../components/TextField.jsx'
import { signIn } from '../services/api.js'

export default function LoginPage({ onLoggedIn, onGoToSignUp, notice }) {
  const [form, setForm] = useState({ name: '', password: '' })
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

    const errs = {}
    if (!form.name.trim()) errs.name = 'Preencha o campo de nome de usuário'
    if (!form.password) errs.password = 'Preencha o campo de senha'
    setFieldErrors(errs)
    if (Object.keys(errs).length) return

    setLoading(true)
    try {
      const token = await signIn({ name: form.name.trim(), password: form.password })
      onLoggedIn(token)
    } catch (err) {
      const apiErrs = err.fieldErrors ?? {}
      setFieldErrors(apiErrs)
      // Erros de campo já aparecem sob o input; o alerta fica para erros gerais
      setError(Object.keys(apiErrs).length ? '' : err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="card">
      <h1>Entrar</h1>
      <p className="subtitle">Acesse sua conta do BooksArchive</p>

      {notice && <div className="alert alert-success">{notice}</div>}
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
          label="Senha"
          name="password"
          type="password"
          autoComplete="current-password"
          value={form.password}
          onChange={handleChange}
          error={fieldErrors.password}
        />
        <button type="submit" className="btn-primary" disabled={loading}>
          {loading ? 'Entrando…' : 'Entrar'}
        </button>
      </form>

      <p className="switch">
        Não tem conta?{' '}
        <button type="button" className="link" onClick={onGoToSignUp}>
          Criar conta
        </button>
      </p>
    </div>
  )
}
