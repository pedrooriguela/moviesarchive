import VideoPlayer from '../components/VideoPlayer.jsx'

// Tela aberta pelo botão de play. A visualização já foi registrada no histórico
// pela ficha do filme (MoviePage) antes de chegar aqui.
export default function PlayerPage({ movie, onBack }) {
  return (
    <section className="window home-window" aria-labelledby="player-titulo">
      <div className="window-title">
        <h1 id="player-titulo">Assistindo: {movie.title}</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← Voltar à ficha
        </button>
      </nav>

      <div className="window-body player-body">
        <VideoPlayer movie={movie} />
        <p className="player-note">Este filme foi salvo no seu histórico de visualizações.</p>
      </div>
    </section>
  )
}
