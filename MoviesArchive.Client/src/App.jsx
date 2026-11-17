import { useEffect, useState } from 'react'
import LandingPage from './pages/LandingPage.jsx'
import HomePage from './pages/HomePage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import SignUpPage from './pages/SignUpPage.jsx'
import ProfilePage from './pages/ProfilePage.jsx'
import ForgotPasswordPage from './pages/ForgotPasswordPage.jsx'
import ResetPasswordPage from './pages/ResetPasswordPage.jsx'
import ListsPage from './pages/ListsPage.jsx'
import MoviePage from './pages/MoviePage.jsx'
import PlayerPage from './pages/PlayerPage.jsx'
import HistoryPage from './pages/HistoryPage.jsx'
import PlansPage from './pages/PlansPage.jsx'
import SubscriptionPage from './pages/SubscriptionPage.jsx'
import AdminPage from './pages/AdminPage.jsx'
import Avatar from './components/Avatar.jsx'
import Dialog from './components/Dialog.jsx'
import NotificationBell from './components/NotificationBell.jsx'
import { clearToken, getUserFromToken, isTokenExpiring, loadToken, saveToken } from './services/token.js'
import { getMyProfile, logout, onSessionChange, refreshSession } from './services/api.js'
import './App.css'

// Mesma rota usada pelo back-end ao montar o link do e-mail (PasswordResetService)
const RESET_PATH = '/redefinir-senha'

// Lê a URL uma vez: se veio do link do e-mail, abre direto a tela de nova senha
function getInitialRoute() {
  const resetToken = new URLSearchParams(window.location.search).get('token')
  if (window.location.pathname === RESET_PATH) return { view: 'reset', resetToken }
  return { view: 'landing', resetToken: null }
}

const initialRoute = getInitialRoute()

const DIALOG_TITLES = { login: 'Entrar', signup: 'Criar nova conta' }

// Texto do "Voltar" da ficha, conforme a tela de onde o filme foi aberto
const MOVIE_BACK_LABELS = { lists: 'Voltar às listas', history: 'Voltar ao histórico' }

// Texto do "Voltar" dos planos, conforme a tela de onde a pessoa veio
const PLANS_BACK_LABELS = { movie: 'Voltar à ficha', subscription: 'Voltar à minha assinatura' }

function App() {
  const [token, setToken] = useState(() => loadToken())
  // Ao abrir o site com o JWT vencido, tenta retomar a sessão pelo refresh token antes de decidir
  const [restoring, setRestoring] = useState(() => {
    const stored = loadToken()
    return Boolean(stored && getUserFromToken(stored, { allowExpired: true }) && isTokenExpiring(stored))
  })
  const [view, setView] = useState(initialRoute.view) // 'landing' | 'forgot' | 'reset'
  const [dialog, setDialog] = useState(null) // null | 'login' | 'signup' (só para visitantes)
  const [signUpHint, setSignUpHint] = useState('') // título que o visitante quis abrir
  const [resetToken] = useState(initialRoute.resetToken)
  const [notice, setNotice] = useState('')
  const [landingNotice, setLandingNotice] = useState('') // aviso na vitrine, ex.: conta excluída
  const [profile, setProfile] = useState(null)
  const [authenticatedView, setAuthenticatedView] = useState('home') // 'home' | 'profile' | 'lists' | 'movie' | 'player' | 'history' | 'plans' | 'subscription' | 'admin'
  const [selectedMovieId, setSelectedMovieId] = useState(null)
  const [movieOrigin, setMovieOrigin] = useState('home') // tela para onde o "Voltar" da ficha leva
  // Avaliação/resposta a destacar ao abrir a ficha por uma notificação ({ reviewId, replyId, nonce })
  const [movieFocus, setMovieFocus] = useState(null)
  const [playingMovie, setPlayingMovie] = useState(null) // filme aberto no player
  const [homeVisit, setHomeVisit] = useState(0) // muda a cada volta ao início, para zerar a busca
  const [plansNotice, setPlansNotice] = useState('') // aviso no topo dos planos, ex.: vindo do play
  const [plansReturn, setPlansReturn] = useState('home') // para onde o "Voltar" dos planos leva
  const [selectedListId, setSelectedListId] = useState(null) // lista aberta em "Minhas listas", para voltar a ela

  // O JWT vencido não encerra a sessão: o authFetch o renova. Quem encerra é o refresh token recusado.
  const user = restoring ? null : getUserFromToken(token, { allowExpired: true })
  const activeUser = { ...user, ...profile }

  // Remove do storage um token que nem dá para ler
  useEffect(() => {
    if (!restoring && token && !user) clearToken()
  }, [restoring, token, user])

  // Foto, nome atualizado e se é administrador: o botão do perfil no topo e o menu dependem disso
  const userId = user?.id
  useEffect(() => {
    if (!userId) return
    let active = true
    getMyProfile(token)
      .then((data) => {
        if (active) setProfile(data)
      })
      .catch(() => {
        // Sem o perfil, o topo mostra a inicial do nome do token; a tela de perfil tenta de novo
      })
    return () => {
      active = false
    }
    // Só ao entrar (ou trocar de conta): o token renovado a cada 15 minutos não muda o perfil
  }, [userId])

  useEffect(() => {
    if (!restoring) return
    refreshSession()
      .then((newToken) => setToken(newToken))
      .catch(() => {
        clearToken()
        setToken(null)
      })
      .finally(() => setRestoring(false))
  }, [restoring])

  // JWT renovado no meio do uso, ou sessão encerrada (refresh token vencido ou revogado)
  useEffect(
    () =>
      onSessionChange((event, newToken) => {
        if (event === 'refreshed') {
          setToken(newToken)
          return
        }
        setToken(null)
        setProfile(null)
        setAuthenticatedView('home')
        setView('landing')
        setLandingNotice('Sua sessão expirou. Entre de novo para continuar.')
      }),
    [],
  )

  // Tira o token da barra de endereço ao sair da tela de nova senha
  function leaveResetRoute() {
    if (window.location.pathname === RESET_PATH) window.history.replaceState(null, '', '/')
  }

  function openLogin(message = '') {
    setNotice(message)
    setLandingNotice('')
    setDialog('login')
  }

  function openSignUp(hint = '') {
    setSignUpHint(hint)
    setLandingNotice('')
    setDialog('signup')
  }

  function closeDialog() {
    setDialog(null)
  }

  function openForgotPassword() {
    leaveResetRoute()
    setDialog(null)
    setView('forgot')
  }

  function backToLanding() {
    leaveResetRoute()
    setView('landing')
  }

  function handleLoggedIn(newToken) {
    saveToken(newToken)
    setToken(newToken)
    setProfile(null)
    setSelectedListId(null)
    setAuthenticatedView('home')
    setNotice('')
    setDialog(null)
  }

  function handleLogout() {
    logout()
    clearToken()
    setToken(null)
    setProfile(null)
    setAuthenticatedView('home')
    setDialog(null)
    setView('landing')
  }

  function handleAccountDeleted() {
    handleLogout()
    setLandingNotice('Sua conta foi excluída.')
  }

  function goHome() {
    setAuthenticatedView('home')
    setHomeVisit((count) => count + 1)
  }

  function openMovie(movieId, origin = 'home', focus = null) {
    setSelectedMovieId(movieId)
    setMovieOrigin(origin)
    setMovieFocus(focus)
    setAuthenticatedView('movie')
    window.scrollTo(0, 0)
  }

  function leaveMovie() {
    if (movieOrigin === 'lists') setAuthenticatedView('lists')
    else if (movieOrigin === 'history') setAuthenticatedView('history')
    else goHome()
  }

  // Minhas listas, histórico, assinatura, administração e sair ficam dentro do perfil
  function openProfile() {
    setAuthenticatedView('profile')
    window.scrollTo(0, 0)
  }

  function openFromProfile(view) {
    setAuthenticatedView(view)
    window.scrollTo(0, 0)
  }

  // Chamado pela ficha depois de registrar a visualização no histórico
  function openPlayer(movie) {
    setPlayingMovie(movie)
    setAuthenticatedView('player')
    window.scrollTo(0, 0)
  }

  // Planos: o "Voltar" retorna para a tela de onde a pessoa veio
  function openPlans(from = 'home', message = '') {
    setPlansReturn(from)
    setPlansNotice(message)
    setAuthenticatedView('plans')
    window.scrollTo(0, 0)
  }

  function leavePlans() {
    if (plansReturn === 'movie') setAuthenticatedView('movie')
    else if (plansReturn === 'subscription') setAuthenticatedView('subscription')
    else goHome()
  }

  function openSubscription() {
    setAuthenticatedView('subscription')
    window.scrollTo(0, 0)
  }

  return (
      <main className="app">
        <header className="app-header">
          {user ? (
              <button type="button" className="brand brand-button" onClick={goHome}>
                Movies Archive
              </button>
          ) : (
              <div className="brand">Movies Archive</div>
          )}

          {!user && !restoring && view === 'landing' && (
              <nav className="visitor-nav" aria-label="Acesso à conta">
                <button type="button" className="btn-secondary" onClick={() => openLogin()}>
                  Entrar
                </button>
                <button type="button" className="btn-primary" onClick={() => openSignUp()}>
                  Criar conta
                </button>
              </nav>
          )}

          {user && view !== 'reset' && (
              <div className="header-actions">
              <NotificationBell
                  token={token}
                  onOpen={(notification) =>
                      openMovie(notification.movieId, 'home', {
                        reviewId: notification.reviewId,
                        replyId: notification.replyId,
                        nonce: notification.id,
                      })
                  }
              />
              {/* Única entrada para o perfil, onde ficam listas, histórico, assinatura e sair */}
              <button
                  type="button"
                  className={authenticatedView === 'profile' ? 'profile-menu-button is-current' : 'profile-menu-button'}
                  onClick={openProfile}
                  aria-label="Meu perfil"
                  aria-current={authenticatedView === 'profile' ? 'page' : undefined}
                  title="Abrir meu perfil"
                  disabled={restoring}
              >
                <span className="profile-menu-photo">
                  <Avatar name={activeUser.name ?? ''} photoUrl={activeUser.photoUrl} size={48} />
                </span>
                <span className="profile-menu-text">
                  <span className="profile-menu-name">{activeUser.name}</span>
                  <span className="profile-menu-hint">
                    {activeUser.isAdmin ? 'Meu perfil · Admin' : 'Meu perfil'} <span aria-hidden="true">▸</span>
                  </span>
                </span>
              </button>
              </div>
          )}
        </header>

        {view === 'reset' ? (
            // O link do e-mail tem prioridade, mesmo com alguém logado no navegador
            <ResetPasswordPage
                resetToken={resetToken}
                onPasswordReset={(msg) => {
                  leaveResetRoute()
                  handleLogout()
                  openLogin(msg)
                }}
                onGoToForgotPassword={openForgotPassword}
            />
        ) : restoring ? (
            <section className="window home-window" aria-labelledby="sessao-titulo">
              <div className="window-title">
                <h1 id="sessao-titulo">Movies Archive</h1>
              </div>
              <div className="window-body">
                <div className="progress" role="status">
                  <div className="progress-track" aria-hidden="true">
                    <span className="progress-bar" />
                  </div>
                  <span>Retomando sua sessão...</span>
                </div>
              </div>
            </section>
        ) : user ? (
            authenticatedView === 'profile' ? (
                <ProfilePage
                    token={token}
                    user={activeUser}
                    onBack={goHome}
                    onProfileUpdated={setProfile}
                    onAccountDeleted={handleAccountDeleted}
                    onOpenLists={() => openFromProfile('lists')}
                    onOpenHistory={() => openFromProfile('history')}
                    onOpenSubscription={() => openFromProfile('subscription')}
                    onOpenAdmin={() => openFromProfile('admin')}
                    onLogout={handleLogout}
                />
            ) : authenticatedView === 'movie' ? (
                <MoviePage
                    key={`${selectedMovieId}-${movieFocus?.nonce ?? ''}`}
                    movieId={selectedMovieId}
                    focus={movieFocus}
                    token={token}
                    user={activeUser}
                    backLabel={MOVIE_BACK_LABELS[movieOrigin] ?? 'Voltar ao início'}
                    onBack={leaveMovie}
                    onPlay={openPlayer}
                    onRequirePlan={(movie) => openPlans('movie', `Assine um plano para assistir a ${movie.title}.`)}
                />
            ) : authenticatedView === 'plans' ? (
                <PlansPage
                    token={token}
                    notice={plansNotice}
                    backLabel={PLANS_BACK_LABELS[plansReturn] ?? 'Voltar ao início'}
                    onBack={leavePlans}
                    onOpenSubscription={openSubscription}
                    onOpenProfile={openProfile}
                />
            ) : authenticatedView === 'subscription' ? (
                <SubscriptionPage
                    token={token}
                    user={activeUser}
                    onBack={openProfile}
                    onOpenPlans={() => openPlans('subscription')}
                    onOpenProfile={openProfile}
                />
            ) : authenticatedView === 'player' && playingMovie ? (
                <PlayerPage
                    movie={playingMovie}
                    onBack={() => setAuthenticatedView('movie')}
                />
            ) : authenticatedView === 'history' ? (
                <HistoryPage
                    token={token}
                    onBack={openProfile}
                    onExplore={goHome}
                    onOpenMovie={(movieId) => openMovie(movieId, 'history')}
                />
            ) : authenticatedView === 'lists' ? (
                <ListsPage
                    token={token}
                    initialListId={selectedListId}
                    onSelectList={setSelectedListId}
                    onBack={openProfile}
                    onOpenMovie={(movieId) => openMovie(movieId, 'lists')}
                />
            ) : authenticatedView === 'admin' && activeUser.isAdmin ? (
                <AdminPage
                    token={token}
                    user={activeUser}
                    onBack={openProfile}
                />
            ) : (
                <HomePage
                    key={homeVisit}
                    token={token}
                    user={activeUser}
                    onOpenMovie={openMovie}
                />
            )
        ) : view === 'forgot' ? (
            <ForgotPasswordPage
                onGoToLogin={() => {
                  backToLanding()
                  openLogin()
                }}
            />
        ) : (
            <LandingPage notice={landingNotice} onSignUp={openSignUp} onLogin={() => openLogin()} />
        )}

        {!user && dialog && (
            <Dialog title={DIALOG_TITLES[dialog]} onClose={closeDialog}>
              {dialog === 'login' ? (
                  <LoginPage
                      notice={notice}
                      onLoggedIn={handleLoggedIn}
                      onGoToSignUp={() => openSignUp()}
                      onGoToForgotPassword={openForgotPassword}
                  />
              ) : (
                  <SignUpPage
                      hint={signUpHint}
                      onLoggedIn={handleLoggedIn}
                      onSignedUp={(msg) => openLogin(msg)}
                      onGoToLogin={() => openLogin()}
                  />
              )}
            </Dialog>
        )}
      </main>
  )
}

export default App
