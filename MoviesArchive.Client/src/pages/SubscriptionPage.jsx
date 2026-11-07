import { useEffect, useState } from 'react'
import Avatar from '../components/Avatar.jsx'
import Dialog from '../components/Dialog.jsx'
import StatusBar from '../components/StatusBar.jsx'
import {
  acceptInvite,
  cancelInvite,
  cancelSubscription,
  declineInvite,
  formatPeople,
  formatPrice,
  getSubscriptionState,
  inviteToPlan,
  leaveSubscription,
  removeMember,
} from '../services/subscriptions.js'
import { formatDateTime } from '../utils/dates.js'

export default function SubscriptionPage({ token, user, onBack, onOpenPlans, onOpenProfile }) {
  const [data, setData] = useState({ status: 'loading', state: null, error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [busy, setBusy] = useState(null) // ação em andamento, para travar só o botão dela
  const [failure, setFailure] = useState(null) // { scope, message }: onde mostrar o erro
  const [flash, setFlash] = useState('')
  const [confirm, setConfirm] = useState(null) // { kind: 'cancel' | 'leave' | 'remove', member? }
  const [inviteName, setInviteName] = useState('')

  useEffect(() => {
    let active = true
    getSubscriptionState(token)
      .then((state) => {
        if (active) setData({ status: 'ready', state, error: '' })
      })
      .catch((requestError) => {
        if (active) setData({ status: 'error', state: null, error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [token, reloadKey])

  // Toda ação devolve a situação atualizada; basta redesenhar
  async function run(key, scope, action, successMessage) {
    setBusy(key)
    setFailure(null)
    setFlash('')
    try {
      const next = await action()
      setData({ status: 'ready', state: next, error: '' })
      if (successMessage) setFlash(successMessage)
      return true
    } catch (requestError) {
      setFailure({ scope, message: requestError.message })
      return false
    } finally {
      setBusy(null)
    }
  }

  async function handleInvite(event) {
    event.preventDefault()
    const name = inviteName.trim()
    if (!name) {
      setFailure({ scope: 'invite', message: 'Informe o nome de usuário de quem você quer convidar.' })
      return
    }
    const ok = await run('invite', 'invite', () => inviteToPlan(name, token), `Convite enviado para ${name}.`)
    if (ok) setInviteName('')
  }

  async function handleConfirm() {
    const { kind, member } = confirm
    const ok =
      kind === 'cancel'
        ? await run('cancel', 'dialog', () => cancelSubscription(token), 'Assinatura cancelada. Sua conta continua, agora gratuita.')
        : kind === 'leave'
          ? await run('leave', 'dialog', () => leaveSubscription(token), 'Você saiu do plano. Sua conta continua, agora gratuita.')
          : await run('remove', 'dialog', () => removeMember(member.userId, token), `${member.name} saiu do plano.`)
    if (ok) setConfirm(null)
  }

  function closeConfirm() {
    if (busy) return
    setConfirm(null)
    setFailure(null)
  }

  function errorFor(scope) {
    return failure?.scope === scope ? (
      <div className="alert alert-error" role="alert">
        {failure.message}
      </div>
    ) : null
  }

  function renderReceivedInvites(state) {
    if (state.receivedInvites.length === 0) return null
    const subscribed = Boolean(state.subscription)

    return (
      <fieldset className="group-box">
        <legend>Convites recebidos</legend>
        <div className="invite-list">
          {state.receivedInvites.map((invite) => (
            <div key={invite.id} className="invite-card">
              <p>
                <strong>{invite.ownerName}</strong> convidou você para o plano <strong>{invite.planName}</strong>.
                <span className="invite-expiry"> Vale até {formatDateTime(invite.expiresAt)}.</span>
              </p>
              {subscribed && (
                <p className="field-hint">Para aceitar, cancele o seu plano ou saia do plano atual.</p>
              )}
              {!state.hasBirthDate && (
                <p className="field-hint">
                  Para aceitar, informe sua data de nascimento no{' '}
                  <button type="button" className="link" onClick={onOpenProfile}>
                    perfil
                  </button>
                  .
                </p>
              )}
              <div className="invite-actions">
                <button
                  type="button"
                  className="btn-primary"
                  disabled={subscribed || !state.hasBirthDate || busy !== null}
                  onClick={() =>
                    run('accept', 'invites', () => acceptInvite(invite.id, token), `Você entrou no plano de ${invite.ownerName}.`)
                  }
                >
                  {busy === 'accept' ? 'Aceitando…' : 'Aceitar'}
                </button>
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={busy !== null}
                  onClick={() => run('decline', 'invites', () => declineInvite(invite.id, token), 'Convite recusado.')}
                >
                  Recusar
                </button>
              </div>
            </div>
          ))}
        </div>
        {errorFor('invites')}
      </fieldset>
    )
  }

  function renderMembers(subscription) {
    const hasMinor = subscription.members.some((member) => member.isMinor)
    return (
      <fieldset className="group-box">
        <legend>
          Integrantes ({subscription.members.length} de {subscription.plan.maxMembers})
        </legend>
        <ul className="member-list">
          {subscription.members.map((member) => (
            <li key={member.userId} className="member-row">
              <Avatar name={member.name} photoUrl={member.photoUrl} size={36} />
              <div>
                <span className="member-name">{member.name}</span>
                <span className="member-tags">
                  {member.isOwner && <span className="badge">Titular</span>}
                  {member.userId === user?.id && <span className="badge">Você</span>}
                  {member.isMinor && <span className="badge">Menor de idade</span>}
                </span>
              </div>
              {subscription.isOwner && !member.isOwner && (
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={busy !== null}
                  onClick={() => setConfirm({ kind: 'remove', member })}
                >
                  Remover
                </button>
              )}
            </li>
          ))}
        </ul>
        {hasMinor && (
          <p className="field-hint">Integrantes menores de idade só veem filmes da classificação deles.</p>
        )}
      </fieldset>
    )
  }

  function renderInvites(subscription) {
    if (!subscription.isOwner) return null

    if (subscription.plan.type !== 'Family') {
      return (
        <div className="notice-with-action">
          <p className="field-hint">
            Quer dividir com mais gente? O plano familiar permite até 4 pessoas, cada uma com a própria conta.
          </p>
          <button type="button" className="btn-secondary" onClick={onOpenPlans}>
            Ver planos
          </button>
        </div>
      )
    }

    const full = subscription.availableSlots === 0
    return (
      <fieldset className="group-box">
        <legend>Convidar para o plano</legend>
        <form className="invite-form" onSubmit={handleInvite} noValidate>
          <div className="field">
            <label htmlFor="convite-nome">Nome de usuário</label>
            <input
              id="convite-nome"
              value={inviteName}
              onChange={(event) => setInviteName(event.target.value)}
              disabled={full}
              autoComplete="off"
            />
          </div>
          <button type="submit" className="btn-primary" disabled={full || busy !== null}>
            {busy === 'invite' ? 'Enviando…' : 'Convidar'}
          </button>
        </form>
        <p className="field-hint">
          {full
            ? 'O plano está completo. Remova alguém ou cancele um convite para chamar outra pessoa.'
            : `${subscription.availableSlots} ${subscription.availableSlots === 1 ? 'vaga livre' : 'vagas livres'}. A pessoa precisa ter conta no site e não ter plano próprio para aceitar.`}
        </p>
        {errorFor('invite')}

        {subscription.pendingInvites.length > 0 && (
          <>
            <h3 className="group-subtitle">Aguardando resposta</h3>
            <ul className="member-list">
              {subscription.pendingInvites.map((invite) => (
                <li key={invite.id} className="member-row">
                  <Avatar name={invite.userName} size={36} />
                  <div>
                    <span className="member-name">{invite.userName}</span>
                    <span className="member-tags">
                      <span className="field-hint">Vale até {formatDateTime(invite.expiresAt)}</span>
                    </span>
                  </div>
                  <button
                    type="button"
                    className="btn-secondary"
                    disabled={busy !== null}
                    onClick={() =>
                      run('cancel-invite', 'invite', () => cancelInvite(invite.id, token), `Convite para ${invite.userName} cancelado.`)
                    }
                  >
                    Cancelar convite
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
      </fieldset>
    )
  }

  function renderSubscription(subscription) {
    return (
      <>
        <div className="subscription-summary">
          <div>
            <h2 className="subscription-plan">{subscription.plan.name}</h2>
            <p className="subscription-meta">
              Desde {formatDateTime(subscription.startedAt)}.{' '}
              {subscription.isOwner ? 'Você é o titular.' : `Titular: ${subscription.ownerName}.`}
            </p>
          </div>
          <p className="subscription-price">
            {formatPrice(subscription.plan.price)} <small>por mês</small>
          </p>
        </div>

        {renderMembers(subscription)}
        {renderInvites(subscription)}

        <div className="subscription-actions">
          {subscription.isOwner ? (
            <>
              <button type="button" className="btn-secondary" onClick={onOpenPlans}>
                Trocar de plano
              </button>
              <button
                type="button"
                className="btn-danger"
                disabled={busy !== null}
                onClick={() => setConfirm({ kind: 'cancel' })}
              >
                Cancelar assinatura
              </button>
            </>
          ) : (
            <button
              type="button"
              className="btn-danger"
              disabled={busy !== null}
              onClick={() => setConfirm({ kind: 'leave' })}
            >
              Sair do plano
            </button>
          )}
        </div>
      </>
    )
  }

  function renderBody() {
    if (data.status === 'loading') {
      return (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Carregando sua assinatura...</span>
        </div>
      )
    }

    if (data.status === 'error') {
      return (
        <div className="results-state">
          <div className="alert alert-error" role="alert">
            {data.error}
          </div>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => {
              setData({ status: 'loading', state: null, error: '' })
              setReloadKey((key) => key + 1)
            }}
          >
            Tentar novamente
          </button>
        </div>
      )
    }

    const { state } = data
    return (
      <div className="subscription-layout">
        {renderReceivedInvites(state)}
        {state.subscription ? (
          renderSubscription(state.subscription)
        ) : (
          <div className="results-state">
            <p>
              Você está na conta gratuita: busca no acervo, abre as fichas, avalia e monta listas, mas não assiste
              aos filmes.
            </p>
            <button type="button" className="btn-primary" onClick={onOpenPlans}>
              Ver planos
            </button>
          </div>
        )}
      </div>
    )
  }

  function renderConfirm() {
    if (!confirm) return null
    const subscription = data.state?.subscription
    const others = subscription ? subscription.members.length - 1 : 0

    const content = {
      cancel: {
        title: 'Cancelar assinatura',
        text: `O plano ${subscription?.plan.name} termina agora.${
          others > 0 ? ` ${others === 1 ? 'A outra pessoa do plano também perde' : `As outras ${formatPeople(others)} do plano também perdem`} o acesso aos filmes.` : ''
        } Sua conta continua, como gratuita.`,
        action: 'Cancelar assinatura',
      },
      leave: {
        title: 'Sair do plano',
        text: `Você deixa o plano ${subscription?.plan.name} de ${subscription?.ownerName} e perde o acesso aos filmes. Para voltar, o titular precisa convidar você de novo.`,
        action: 'Sair do plano',
      },
      remove: {
        title: 'Remover integrante',
        text: `${confirm.member?.name} sai do plano e perde o acesso aos filmes. A conta continua, como gratuita.`,
        action: 'Remover',
      },
    }[confirm.kind]

    return (
      <Dialog title={content.title} onClose={closeConfirm}>
        <p className="dialog-lead">{content.text}</p>
        {errorFor('dialog')}
        <div className="dialog-actions">
          <button type="button" className="btn-danger dialog-submit" onClick={handleConfirm} disabled={busy !== null}>
            {busy ? 'Aguarde…' : content.action}
          </button>
          <button type="button" className="btn-secondary" onClick={closeConfirm} disabled={busy !== null}>
            Voltar
          </button>
        </div>
      </Dialog>
    )
  }

  const subscription = data.state?.subscription
  return (
    <section className="window home-window" aria-labelledby="assinatura-titulo">
      <div className="window-title">
        <h1 id="assinatura-titulo">Minha assinatura</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← Voltar ao início
        </button>
        <button type="button" onClick={onOpenPlans}>
          Ver planos
        </button>
      </nav>

      <div className="window-body window-body-spaced">
        {flash && (
          <div className="alert alert-success" role="status">
            {flash}
          </div>
        )}
        {renderBody()}
      </div>

      <StatusBar
        items={[
          data.status === 'ready' && (subscription ? `Plano ${subscription.plan.name}` : 'Conta gratuita'),
          subscription && `${formatPeople(subscription.members.length)} no plano`,
          subscription?.pendingInvites.length > 0 &&
            `${subscription.pendingInvites.length} ${subscription.pendingInvites.length === 1 ? 'convite pendente' : 'convites pendentes'}`,
          data.state?.receivedInvites.length > 0 &&
            `${data.state.receivedInvites.length} ${data.state.receivedInvites.length === 1 ? 'convite recebido' : 'convites recebidos'}`,
        ]}
      />

      {renderConfirm()}
    </section>
  )
}
