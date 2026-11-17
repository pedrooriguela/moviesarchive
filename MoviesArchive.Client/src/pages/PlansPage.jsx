import { useEffect, useState } from 'react'
import Dialog from '../components/Dialog.jsx'
import StatusBar from '../components/StatusBar.jsx'
import {
  changePlan,
  formatPeople,
  formatPrice,
  getPlans,
  getSubscriptionState,
  subscribe,
} from '../services/subscriptions.js'

// O que cada carteirinha promete. Só recursos que existem no site.
const FREE_PERKS = [
  { included: true, text: 'Buscar no acervo e abrir as fichas' },
  { included: true, text: 'Avaliar filmes e montar listas' },
  { included: false, text: 'Assistir aos filmes' },
]

function perksFor(plan) {
  if (plan.type === 'Family') {
    return [
      { included: true, text: 'Tudo da conta gratuita' },
      { included: true, text: 'Assistir aos filmes' },
      { included: true, text: `Até ${formatPeople(plan.maxMembers)}, titular incluído` },
      { included: true, text: 'Cada integrante usa a própria conta' },
      { included: true, text: 'Menores veem só filmes da idade deles' },
    ]
  }
  return [
    { included: true, text: 'Tudo da conta gratuita' },
    { included: true, text: 'Assistir aos filmes' },
    { included: true, text: 'Para uma pessoa' },
  ]
}

function taglineFor(plan) {
  return plan.type === 'Family' ? `Para até ${formatPeople(plan.maxMembers)}` : 'Para uma pessoa'
}

function Perks({ perks }) {
  return (
    <ul className="plan-perks">
      {perks.map((perk) => (
        <li key={perk.text} className={perk.included ? 'plan-perk plan-perk-yes' : 'plan-perk plan-perk-no'}>
          <span className="plan-perk-mark" aria-hidden="true">
            {perk.included ? '✓' : '✗'}
          </span>
          <span>
            {perk.included ? '' : <span className="visually-hidden">Não inclui: </span>}
            {perk.text}
          </span>
        </li>
      ))}
    </ul>
  )
}

export default function PlansPage({ token, notice, backLabel, onBack, onOpenSubscription, onOpenProfile }) {
  const [data, setData] = useState({ status: 'loading', plans: [], state: null, error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [choice, setChoice] = useState(null) // { plan, kind: 'subscribe' | 'change' } à espera de confirmação
  const [submitting, setSubmitting] = useState(false)
  const [dialogError, setDialogError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => {
    let active = true
    Promise.all([getPlans(token), getSubscriptionState(token)])
      .then(([plans, state]) => {
        if (active) setData({ status: 'ready', plans, state, error: '' })
      })
      .catch((requestError) => {
        if (active) setData({ status: 'error', plans: [], state: null, error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [token, reloadKey])

  const state = data.state
  const subscription = state?.subscription ?? null

  // O botão de cada carteirinha depende da situação da pessoa
  function actionFor(plan) {
    if (subscription) {
      if (subscription.plan.id === plan.id) return { label: 'Seu plano atual', disabled: true }
      if (!subscription.isOwner) {
        return { label: `Trocar para ${plan.name}`, disabled: true, note: 'Só o titular pode trocar o plano.' }
      }
      return { label: `Trocar para ${plan.name}`, kind: 'change' }
    }
    if (!state.hasBirthDate) {
      return { label: `Assinar ${plan.name}`, disabled: true, note: 'Informe sua data de nascimento no perfil.' }
    }
    if (!state.isAdult) {
      return { label: `Assinar ${plan.name}`, disabled: true, note: 'Só maiores de 18 anos contratam planos.' }
    }
    return { label: `Assinar ${plan.name}`, kind: 'subscribe' }
  }

  function closeDialog() {
    if (submitting) return
    setChoice(null)
    setDialogError('')
  }

  async function confirmChoice() {
    setSubmitting(true)
    setDialogError('')
    try {
      const next =
        choice.kind === 'change' ? await changePlan(choice.plan.id, token) : await subscribe(choice.plan.id, token)
      setData((current) => ({ ...current, state: next }))
      setSuccess(
        choice.kind === 'change'
          ? `Plano trocado para ${choice.plan.name}.`
          : `Pronto: você assinou o plano ${choice.plan.name}. Os filmes estão liberados.`,
      )
      setChoice(null)
    } catch (requestError) {
      setDialogError(requestError.message)
    } finally {
      setSubmitting(false)
    }
  }

  function renderNotices() {
    if (subscription) return null

    if (!state.hasBirthDate) {
      return (
        <div className="alert alert-info notice-with-action" role="status">
          <span>Para assinar, informe sua data de nascimento no perfil. Ela não pode ser mudada depois.</span>
          <button type="button" className="btn-secondary" onClick={onOpenProfile}>
            Ir para o perfil
          </button>
        </div>
      )
    }

    if (!state.isAdult) {
      return (
        <div className="alert alert-info" role="status">
          Planos só podem ser contratados por maiores de 18 anos. Um adulto pode convidar você para o plano
          familiar dele; o convite aparece em Minha assinatura.
        </div>
      )
    }

    return null
  }

  function renderFreeCard() {
    const isCurrent = !subscription
    return (
      <article className={isCurrent ? 'plan-card plan-card-free plan-card-current' : 'plan-card plan-card-free'}>
        <header className="plan-card-header">
          <h2 className="plan-card-name">Gratuito</h2>
          <p className="plan-card-tagline">A sua conta, sem plano</p>
        </header>
        <p className="plan-card-price">
          {formatPrice(0)}
        </p>
        <Perks perks={FREE_PERKS} />
        <div className="plan-card-footer">
          {isCurrent ? (
            <button type="button" className="btn-secondary" disabled>
              Seu plano atual
            </button>
          ) : (
            <p className="plan-card-note">
              {subscription.isOwner
                ? 'Para voltar ao gratuito, cancele a assinatura em Minha assinatura.'
                : 'Para voltar ao gratuito, saia do plano em Minha assinatura.'}
            </p>
          )}
        </div>
      </article>
    )
  }

  function renderPlanCard(plan) {
    const action = actionFor(plan)
    const isCurrent = subscription?.plan.id === plan.id
    return (
      <article key={plan.id} className={isCurrent ? 'plan-card plan-card-current' : 'plan-card'}>
        <header className="plan-card-header">
          <h2 className="plan-card-name">{plan.name}</h2>
          <p className="plan-card-tagline">{taglineFor(plan)}</p>
        </header>
        <p className="plan-card-price">
          {formatPrice(plan.price)} <small>por mês</small>
        </p>
        <Perks perks={perksFor(plan)} />
        <div className="plan-card-footer">
          <button
            type="button"
            className={action.kind ? 'btn-primary' : 'btn-secondary'}
            disabled={action.disabled}
            onClick={() => setChoice({ plan, kind: action.kind })}
          >
            {action.label}
          </button>
          {action.note && <p className="plan-card-note">{action.note}</p>}
        </div>
      </article>
    )
  }

  function renderBody() {
    if (data.status === 'loading') {
      return (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Carregando os planos...</span>
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
              setData({ status: 'loading', plans: [], state: null, error: '' })
              setReloadKey((key) => key + 1)
            }}
          >
            Tentar novamente
          </button>
        </div>
      )
    }

    return (
      <>
        {renderNotices()}
        <p className="plans-intro">
          Navegar pelo acervo é grátis. Para assistir aos filmes, escolha um plano.
        </p>
        <div className="plans-grid">
          {renderFreeCard()}
          {data.plans.map(renderPlanCard)}
        </div>
        <p className="plans-footnote">
          Valores mensais. O pagamento é simulado: nenhuma cobrança é feita.
        </p>
      </>
    )
  }

  return (
    <section className="window home-window" aria-labelledby="planos-titulo">
      <div className="window-title">
        <h1 id="planos-titulo">Planos</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← {backLabel ?? 'Voltar ao início'}
        </button>
        <button type="button" onClick={onOpenSubscription}>
          Minha assinatura
        </button>
      </nav>

      <div className="window-body window-body-spaced">
        {notice && !subscription && (
          <div className="alert alert-info" role="status">
            {notice}
          </div>
        )}
        {success && (
          <div className="alert alert-success notice-with-action" role="status">
            <span>{success}</span>
            <button type="button" className="btn-secondary" onClick={onOpenSubscription}>
              Ver minha assinatura
            </button>
          </div>
        )}
        {renderBody()}
      </div>

      <StatusBar
        items={[
          data.status === 'ready' && (subscription ? `Plano atual: ${subscription.plan.name}` : 'Conta gratuita'),
          data.status === 'ready' && `${data.plans.length} ${data.plans.length === 1 ? 'plano' : 'planos'} à venda`,
        ]}
      />

      {choice && (
        <Dialog
          title={choice.kind === 'change' ? `Trocar para ${choice.plan.name}` : `Assinar ${choice.plan.name}`}
          onClose={closeDialog}
        >
          <dl className="dialog-summary">
            <div>
              <dt>Plano</dt>
              <dd>{choice.plan.name}</dd>
            </div>
            <div>
              <dt>Valor mensal</dt>
              <dd>{formatPrice(choice.plan.price)}</dd>
            </div>
            <div>
              <dt>Pessoas</dt>
              <dd>
                {choice.plan.type === 'Family'
                  ? `Até ${formatPeople(choice.plan.maxMembers)}, titular incluído`
                  : '1 pessoa'}
              </dd>
            </div>
          </dl>

          <p className="dialog-lead">
            {choice.kind === 'change'
              ? 'A troca vale na hora. Para ir a um plano menor, o plano atual precisa caber nele.'
              : 'O acesso aos filmes é liberado na hora.'}{' '}
            O pagamento é simulado: nenhuma cobrança é feita.
          </p>

          {dialogError && (
            <div className="alert alert-error" role="alert">
              {dialogError}
            </div>
          )}

          <div className="dialog-actions">
            <button type="button" className="btn-primary dialog-submit" onClick={confirmChoice} disabled={submitting}>
              {submitting ? 'Confirmando…' : choice.kind === 'change' ? 'Confirmar troca' : 'Confirmar assinatura'}
            </button>
            <button type="button" className="btn-secondary" onClick={closeDialog} disabled={submitting}>
              Voltar
            </button>
          </div>
        </Dialog>
      )}
    </section>
  )
}
