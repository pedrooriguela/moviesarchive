import MovieArt from './MovieArt.jsx'

// Área onde o filme é exibido.
//
// Os arquivos dos filmes ainda não estão no banco, então por enquanto ela mostra
// só uma tela de aviso. Quando houver um endereço de vídeo, basta trocar o
// conteúdo de .video-player-screen por algo como:
//
//   <video className="video-player-media" src={movie.videoUrl} controls autoPlay />
//
// O botão de play, o registro no histórico e a navegação não precisam mudar.
export default function VideoPlayer({ movie }) {
  return (
    <div className="video-player">
      <div className="video-player-screen">
        <MovieArt movie={movie} variant="player" />
        <div className="video-player-message" role="status">
          <span className="video-player-icon" aria-hidden="true">
            ▶
          </span>
          <p className="video-player-title">{movie.title}</p>
          <p>O player ainda não está disponível. A exibição do filme vai aparecer aqui.</p>
        </div>
      </div>
    </div>
  )
}
