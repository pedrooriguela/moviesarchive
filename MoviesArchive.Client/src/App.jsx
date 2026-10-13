import { useEffect, useState } from 'react'
import HomePage from './pages/HomePage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import SignUpPage from './pages/SignUpPage.jsx'
import ForgotPasswordPage from './pages/ForgotPasswordPage.jsx'
import ResetPasswordPage from './pages/ResetPasswordPage.jsx'
import { clearToken, getUserFromToken, loadToken, saveToken } from './services/token.js'
import './App.css'

// Caminho que o back-end coloca no link do e-mail de redefinição
const RESET_PATH = '/redefinir-senha'

// Se a página foi aberta pelo link do e-mail, começa na tela de nova senha
function getInitialRoute() {
  const resetToken = new URLSearchParams(window.location.search).get('token')
  if (window.location.pathname === RESET_PATH) return { view: 'reset', resetToken }
  return { view: 'login', resetToken: null }
}

function App() {
  const [token, setToken] = useState(() => loadToken())
  const [initialRoute] = useState(getInitialRoute)
  const [view, setView] = useState(initialRoute.view) // 'login' | 'signup' | 'forgot' | 'reset'
  const [resetToken] = useState(initialRoute.resetToken)
  const [notice, setNotice] = useState('')

  const user = getUserFromToken(token)

  // Remove token inválido/expirado do storage
  useEffect(() => {
    if (token && !user) clearToken()
  }, [token, user])

  function handleLoggedIn(newToken) {
    saveToken(newToken)
    setToken(newToken)
    setNotice('')
  }

  function handleLogout() {
    clearToken()
    setToken(null)
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
        <header className="brand">Movies Archive</header>

        {view === 'reset' ? (
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
        ) : view === 'forgot' ? (
            <ForgotPasswordPage onGoToLogin={() => goTo('login')} />
        ) : (
            <LoginPage
                notice={notice}
                onLoggedIn={handleLoggedIn}
                onGoToSignUp={() => goTo('signup')}
                onGoToForgotPassword={() => goTo('forgot')}
            />
        )}
      </main>
  )
}

export default App
