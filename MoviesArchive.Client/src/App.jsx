import { useEffect, useState } from 'react'
import LandingPage from './pages/LandingPage.jsx'
import HomePage from './pages/HomePage.jsx'
import UsersPage from './pages/UsersPage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import SignUpPage from './pages/SignUpPage.jsx'
import ProfilePage from './pages/ProfilePage.jsx'
import ForgotPasswordPage from './pages/ForgotPasswordPage.jsx'
import ResetPasswordPage from './pages/ResetPasswordPage.jsx'
import ListsPage from './pages/ListsPage.jsx'
import MoviePage from './pages/MoviePage.jsx'
import WatchlistPage from './pages/WatchlistPage.jsx'
import PlayerPage from './pages/PlayerPage.jsx'
import HistoryPage from './pages/HistoryPage.jsx'
import Dialog from './components/Dialog.jsx'
import { clearToken, getUserFromToken, loadToken, saveToken } from './services/token.js'
import { getPhotoSrc } from './services/api.js'
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
const MOVIE_BACK_LABELS = { watchlist: 'Voltar à watchlist', history: 'Voltar ao histórico' }

function App() {
  const [token, setToken] = useState(() => loadToken())
  const [view, setView] = useState(initialRoute.view) // 'landing' | 'forgot' | 'reset'
  const [dialog, setDialog] = useState(null) // null | 'login' | 'signup' (só para visitantes)
  const [signUpHint, setSignUpHint] = useState('') // título que o visitante quis abrir
  const [resetToken] = useState(initialRoute.resetToken)
  const [notice, setNotice] = useState('')
  const [landingNotice, setLandingNotice] = useState('') // aviso na vitrine, ex.: conta excluída
  const [profile, setProfile] = useState(null)
  const [authenticatedView, setAuthenticatedView] = useState('home') // 'home' | 'profile' | 'lists' | 'movie' | 'watchlist' | 'player' | 'history'
  const [selectedMovieId, setSelectedMovieId] = useState(null)
  const [movieOrigin, setMovieOrigin] = useState('home') // tela para onde o "Voltar" da ficha leva
  const [playingMovie, setPlayingMovie] = useState(null) // filme aberto no player
  const [homeVisit, setHomeVisit] = useState(0) // muda a cada volta ao início, para zerar a busca

  const user = getUserFromToken(token)
  const activeUser = { ...user, ...profile }

  // Remove token inválido/expirado do storage
  useEffect(() => {
    if (token && !user) clearToken()
  }, [token, user])

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
    setAuthenticatedView('home')
    setNotice('')
    setDialog(null)
  }

  function handleLogout() {
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

  function openMovie(movieId, origin = 'home') {
    setSelectedMovieId(movieId)
    setMovieOrigin(origin)
    setAuthenticatedView('movie')
    window.scrollTo(0, 0)
  }

  function leaveMovie() {
    if (movieOrigin === 'watchlist') setAuthenticatedView('watchlist')
    else if (movieOrigin === 'history') setAuthenticatedView('history')
    else goHome()
  }

  // Chamado pela ficha depois de registrar a visualização no histórico
  function openPlayer(movie) {
    setPlayingMovie(movie)
    setAuthenticatedView('player')
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

          {!user && view === 'landing' && (
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
              <button
                  type="button"
                  className="profile-menu-button"
                  onClick={() => setAuthenticatedView('profile')}
                  aria-label="Abrir perfil"
                  title="Perfil"
              >
                {activeUser.photoUrl ? (
                    <img src={getPhotoSrc(activeUser.photoUrl)} alt="" />
                ) : (
                    <span>{activeUser.name?.trim().charAt(0).toUpperCase() || '?'}</span>
                )}
              </button>
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
        ) : user ? (
            authenticatedView === 'users' ? (
                <UsersPage token={token} onBack={goHome} />
            ) : authenticatedView === 'profile' ? (
                <ProfilePage
                    token={token}
                    onBack={() => setAuthenticatedView('home')}
                    onProfileUpdated={setProfile}
                    onAccountDeleted={handleAccountDeleted}
                />
            ) : authenticatedView === 'movie' ? (
                <MoviePage
                    key={selectedMovieId}
                    movieId={selectedMovieId}
                    token={token}
                    user={activeUser}
                    backLabel={MOVIE_BACK_LABELS[movieOrigin] ?? 'Voltar ao início'}
                    onBack={leaveMovie}
                    onPlay={openPlayer}
                />
            ) : authenticatedView === 'player' && playingMovie ? (
                <PlayerPage
                    movie={playingMovie}
                    onBack={() => setAuthenticatedView('movie')}
                />
            ) : authenticatedView === 'history' ? (
                <HistoryPage
                    token={token}
                    onBack={goHome}
                    onOpenMovie={(movieId) => openMovie(movieId, 'history')}
                />
            ) : authenticatedView === 'watchlist' ? (
                <WatchlistPage
                    token={token}
                    onBack={goHome}
                    onOpenMovie={(movieId) => openMovie(movieId, 'watchlist')}
                />
            ) : authenticatedView === 'lists' ? (
                <ListsPage
                    token={token}
                    onBack={() => setAuthenticatedView('home')}
                />
            ) : (
                <HomePage
                    key={homeVisit}
                    token={token}
                    user={activeUser}
                    onOpenMovie={openMovie}
                    onOpenLists={() => setAuthenticatedView('lists')}
                    onOpenWatchlist={() => setAuthenticatedView('watchlist')}
                    onOpenHistory={() => setAuthenticatedView('history')}
                    onOpenUsers={() => setAuthenticatedView('users')}
                    onOpenProfile={() => setAuthenticatedView('profile')}
                    onLogout={handleLogout}
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
