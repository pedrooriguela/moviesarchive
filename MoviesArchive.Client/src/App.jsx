import { useEffect, useState } from 'react'
import HomePage from './pages/HomePage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import SignUpPage from './pages/SignUpPage.jsx'
import ProfilePage from './pages/ProfilePage.jsx'
import { clearToken, getUserFromToken, loadToken, saveToken } from './services/token.js'
import './App.css'

function App() {
  const [token, setToken] = useState(() => loadToken())
  const [view, setView] = useState('login') // 'login' | 'signup'
  const [notice, setNotice] = useState('')
  const [profile, setProfile] = useState(null)
  const [authenticatedView, setAuthenticatedView] = useState('home')

  const user = getUserFromToken(token)
  const activeUser = { ...user, ...profile }

  // Remove token inválido/expirado do storage
  useEffect(() => {
    if (token && !user) clearToken()
  }, [token, user])

  function handleLoggedIn(newToken) {
    saveToken(newToken)
    setToken(newToken)
    setProfile(null)
    setAuthenticatedView('home')
    setNotice('')
  }

  function handleLogout() {
    clearToken()
    setToken(null)
    setProfile(null)
    setAuthenticatedView('home')
    setView('login')
  }

  function goTo(nextView, message = '') {
    // Ao sair da tela de nova senha, tira o token da barra de endereço
    if (window.location.pathname === RESET_PATH) window.history.replaceState(null, '', '/')
    setNotice(message)
    setView(nextView)
  }

  return (
      <main className="app">
        <header className="app-header">
          <div className="brand">Movies Archive</div>
          {user && (
            <button
              type="button"
              className="profile-menu-button"
              onClick={() => setAuthenticatedView('profile')}
              aria-label="Abrir perfil"
              title="Perfil"
            >
              {activeUser.photoUrl ? (
                <img src={activeUser.photoUrl} alt="" />
              ) : (
                <span>{activeUser.name?.trim().charAt(0).toUpperCase() || '?'}</span>
              )}
            </button>
          )}
        </header>

        {user ? (
            authenticatedView === 'profile' ? (
              <ProfilePage
                token={token}
                onBack={() => setAuthenticatedView('home')}
                onProfileUpdated={setProfile}
              />
            ) : (
              <HomePage
                user={activeUser}
                onLogout={handleLogout}
              />
            )
        ) : view === 'reset' ? (
            // O link do e-mail tem prioridade, mesmo com alguém logado no navegador
            <ResetPasswordPage
                resetToken={resetToken}
                onPasswordReset={(msg) => {
                    handleLogout()
                    goTo('login', msg)
                }}
                onGoToForgotPassword={() => goTo('forgot')}
            />
        ) : user ? (
                <HomePage user={user} onLogout={handleLogout} />
            ) : view === 'signup' ? (
            <SignUpPage
                onLoggedIn={handleLoggedIn}
                onSignedUp={(msg) => goTo('login', msg)}
                onGoToLogin={() => goTo('login')}
            />
        ) : (
            <LoginPage
                notice={notice}
                onLoggedIn={handleLoggedIn}
                onGoToSignUp={() => goTo('signup')}
            />
        )}
      </main>
  )
}

export default App
