import { useEffect, useState } from 'react'
import HomePage from './pages/HomePage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import SignUpPage from './pages/SignUpPage.jsx'
import { clearToken, getUserFromToken, loadToken, saveToken } from './services/token.js'
import './App.css'

function App() {
  const [token, setToken] = useState(() => loadToken())
  const [view, setView] = useState('login') // 'login' | 'signup'
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
    setNotice(message)
    setView(nextView)
  }

  return (
    <main className="app">
      <header className="brand"></header>

      {user ? (
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
