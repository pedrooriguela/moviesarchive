import { useState } from 'react'
import TextField from '../components/TextField.jsx'
import { resetPassword } from '../services/api.js'

// Mesmas regras de senha do cadastro
function validate(form) {
  const errs = {}
  if (!form.password) errs.password = 'A senha é obrigatória'
  else if (form.password.length < 6) errs.password = 'A senha deve ter pelo menos 6 caracteres'
  if (form.confirmPassword !== form.password) errs.confirmPassword = 'As senhas não coincidem'
  return errs
}

export default function ResetPasswordPage({ resetToken, onPasswordReset, onGoToForgotPassword }) {
  const [form, setForm] = useState({ password: '', confirmPassword: '' })
  const [fieldErrors, setFieldErrors] = useState({})
  const [error, setError] = useState('')
  const [linkInvalid, setLinkInvalid] = useState(!resetToken)
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

    setLoading(true)
    try {
      await resetPassword(resetToken, form.password)
      onPasswordReset('Senha alterada! Entre com a sua nova senha.')
    } catch (err) {
      const apiErrs = err.fieldErrors ?? {}
      if (Object.keys(apiErrs).length) {
        // senha recusada pela validação do back-end
        setFieldErrors(apiErrs)
      } else if (err.status === 400 || err.status === 401 || err.status === 404) {
        // token não existe, expirou ou já foi usado
        setLinkInvalid(true)
      } else {
        setError(err.message)
      }
      setLoading(false)
    }
  }

  if (linkInvalid) {
    return (
      <div className="card">
        <h1>Link inválido</h1>
        <div className="alert alert-error">
          Este link de redefinição expirou ou já foi usado. Peça um novo para continuar.
        </div>
        <button type="button" className="btn-primary" onClick={onGoToForgotPassword}>
          Pedir novo link
        </button>
      </div>
    )
  }

  return (
    <div className="card">
      <h1>Nova senha</h1>
      <p className="subtitle">Crie uma nova senha para a sua conta.</p>

      {error && <div className="alert alert-error">{error}</div>}

      <form onSubmit={handleSubmit} noValidate>
        <TextField
          label="Nova senha"
          name="password"
          type="password"
          autoComplete="new-password"
          value={form.password}
          onChange={handleChange}
          error={fieldErrors.password}
          autoFocus
        />
        <TextField
          label="Confirmar nova senha"
          name="confirmPassword"
          type="password"
          autoComplete="new-password"
          value={form.confirmPassword}
          onChange={handleChange}
          error={fieldErrors.confirmPassword}
        />
        <button type="submit" className="btn-primary" disabled={loading}>
          {loading ? 'Salvando…' : 'Salvar nova senha'}
        </button>
      </form>
    </div>
  )
}
