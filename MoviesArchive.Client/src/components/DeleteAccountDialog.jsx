import { useState } from 'react'
import Dialog from './Dialog.jsx'
import TextField from './TextField.jsx'
import { deleteMyAccount } from '../services/api.js'

const DESTINATIONS = [
  {
    value: 'anonymize',
    title: 'Manter meu conteúdo de forma anônima',
    text: 'Avaliações, respostas e listas públicas continuam, assinadas como “Usuário removido”. Listas privadas, favoritos, seguidores e o histórico de filmes assistidos são apagados.',
  },
  {
    value: 'delete',
    title: 'Apagar tudo',
    text: 'Listas, avaliações, respostas, favoritos, seguidores e histórico são removidos junto com a conta.',
  },
]

export default function DeleteAccountDialog({ token, onClose, onDeleted }) {
  const [destination, setDestination] = useState('anonymize')
  const [password, setPassword] = useState('')
  const [passwordError, setPasswordError] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  // Não deixa fechar no meio da exclusão
  function close() {
    if (!loading) onClose()
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setPasswordError('')

    if (!password) {
      setPasswordError('Informe sua senha para confirmar')
      return
    }

    setLoading(true)
    try {
      await deleteMyAccount(token, { password, destination })
      onDeleted()
    } catch (requestError) {
      if (requestError.fieldErrors?.password) setPasswordError(requestError.fieldErrors.password)
      else if (requestError.status === 400) setPasswordError(requestError.message)
      else setError(requestError.message)
      setLoading(false)
    }
  }

  return (
    <Dialog title="Excluir conta" onClose={close}>
      <p className="dialog-lead">
        Esta ação não pode ser desfeita. Escolha o que acontece com o que você criou.
      </p>

      {error && <div className="alert alert-error" role="alert">{error}</div>}

      <form onSubmit={handleSubmit} noValidate>
        <fieldset className="choice-group">
          <legend>Seu conteúdo</legend>
          {DESTINATIONS.map((option) => (
            <label key={option.value} className="choice">
              <input
                type="radio"
                name="destination"
                value={option.value}
                checked={destination === option.value}
                onChange={() => setDestination(option.value)}
              />
              <span className="choice-title">{option.title}</span>
              <span className="choice-text">{option.text}</span>
            </label>
          ))}
        </fieldset>

        <TextField
          label="Confirme sua senha"
          name="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => {
            setPassword(event.target.value)
            setPasswordError('')
          }}
          error={passwordError}
          autoFocus
        />

        <button type="submit" className="btn-primary btn-danger dialog-submit" disabled={loading}>
          {loading ? 'Excluindo…' : 'Excluir minha conta'}
        </button>
        <button type="button" className="btn-secondary" onClick={close} disabled={loading}>
          Cancelar
        </button>
      </form>
    </Dialog>
  )
}
