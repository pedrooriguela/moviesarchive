import { useEffect, useRef, useState } from 'react'
import MovieArt from './MovieArt.jsx'

// Ícones em pixel art (grades 7 x 7), no mesmo traço das estrelas e do coração
const ICONS = {
  play: ['..#....', '..##...', '..###..', '..####.', '..###..', '..##...', '..#....'],
  pause: ['##..##.', '##..##.', '##..##.', '##..##.', '##..##.', '##..##.', '##..##.'],
  stop: ['.......', '######.', '######.', '######.', '######.', '######.', '.......'],
  sound: ['...#...', '..##.#.', '####..#', '####..#', '####..#', '..##.#.', '...#...'],
  muted: ['...#...', '..##...', '####.#.', '####..#', '####.#.', '..##...', '...#...'],
  fullscreen: ['###.###', '#.....#', '#.....#', '.......', '#.....#', '#.....#', '###.###'],
}

function PixelIcon({ name }) {
  const pixels = ICONS[name].flatMap((row, y) =>
    [...row].map((cell, x) => (cell === '#' ? { x, y } : null)).filter(Boolean),
  )
  return (
    <svg className="pixel-icon" viewBox="0 0 7 7" shapeRendering="crispEdges" aria-hidden="true" focusable="false">
      {pixels.map(({ x, y }) => (
        <rect key={`${x}-${y}`} x={x} y={y} width="1" height="1" />
      ))}
    </svg>
  )
}

// 3725 s -> "1:02:05"; 65 s -> "1:05"
function formatTime(seconds) {
  if (!Number.isFinite(seconds) || seconds < 0) return '0:00'
  const total = Math.floor(seconds)
  const h = Math.floor(total / 3600)
  const m = Math.floor((total % 3600) / 60)
  const s = String(total % 60).padStart(2, '0')
  return h > 0 ? `${h}:${String(m).padStart(2, '0')}:${s}` : `${m}:${s}`
}

function spokenTime(seconds) {
  const total = Math.floor(Number.isFinite(seconds) ? seconds : 0)
  const h = Math.floor(total / 3600)
  const m = Math.floor((total % 3600) / 60)
  const s = total % 60
  return [h && `${h} h`, (h || m) && `${m} min`, `${s} s`].filter(Boolean).join(' ')
}

const SEEK_STEP = 10 // segundos nas setas

// Player do filme. O endereço do vídeo (movie.videoUrl) vem da resposta do play,
// que a API só devolve depois de conferir a assinatura.
export default function VideoPlayer({ movie }) {
  const containerRef = useRef(null)
  const videoRef = useRef(null)
  const [playing, setPlaying] = useState(false)
  const [status, setStatus] = useState('loading') // loading | ready | waiting | ended | error
  const [currentTime, setCurrentTime] = useState(0)
  const [duration, setDuration] = useState(0)
  const [volume, setVolume] = useState(1)
  const [muted, setMuted] = useState(false)
  const [fullscreen, setFullscreen] = useState(false)

  useEffect(() => {
    function handleFullscreenChange() {
      setFullscreen(document.fullscreenElement === containerRef.current)
    }
    document.addEventListener('fullscreenchange', handleFullscreenChange)
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange)
  }, [])

  if (!movie.videoUrl) {
    return (
      <div className="video-player">
        <div className="video-player-screen">
          <MovieArt movie={movie} variant="player" />
          <div className="video-player-message" role="status">
            <span className="video-player-icon" aria-hidden="true">
              <PixelIcon name="stop" />
            </span>
            <p className="video-player-title">{movie.title}</p>
            <p>Este filme ainda não tem vídeo no acervo.</p>
          </div>
        </div>
      </div>
    )
  }

  const video = () => videoRef.current

  function togglePlay() {
    const element = video()
    if (!element || status === 'error') return
    if (element.paused || element.ended) {
      element.play().catch(() => setPlaying(false))
    } else {
      element.pause()
    }
  }

  function stop() {
    const element = video()
    if (!element) return
    element.pause()
    element.currentTime = 0
  }

  function seekTo(seconds) {
    const element = video()
    if (!element || !Number.isFinite(element.duration)) return
    element.currentTime = Math.min(Math.max(seconds, 0), element.duration)
  }

  function changeVolume(value) {
    const element = video()
    if (!element) return
    element.volume = value
    element.muted = value === 0
  }

  function toggleMute() {
    const element = video()
    if (!element) return
    element.muted = !element.muted
    if (!element.muted && element.volume === 0) element.volume = 0.5
  }

  function toggleFullscreen() {
    if (document.fullscreenElement) document.exitFullscreen?.()
    else containerRef.current?.requestFullscreen?.()
  }

  // Atalhos com o foco dentro do player (os controles deslizantes cuidam das próprias setas)
  function handleKeyDown(event) {
    const target = event.target
    const onSlider = target instanceof HTMLInputElement && target.type === 'range'
    const onButton = target instanceof HTMLButtonElement

    switch (event.key) {
      case ' ':
      case 'k':
      case 'K':
        if (onButton && event.key === ' ') return // o próprio botão já responde ao espaço
        event.preventDefault()
        togglePlay()
        break
      case 'ArrowLeft':
        if (onSlider) return
        event.preventDefault()
        seekTo(currentTime - SEEK_STEP)
        break
      case 'ArrowRight':
        if (onSlider) return
        event.preventDefault()
        seekTo(currentTime + SEEK_STEP)
        break
      case 'f':
      case 'F':
        event.preventDefault()
        toggleFullscreen()
        break
      case 'm':
      case 'M':
        event.preventDefault()
        toggleMute()
        break
      default:
    }
  }

  const progress = duration > 0 ? (currentTime / duration) * 100 : 0
  const effectiveVolume = muted ? 0 : volume
  const statusText = {
    loading: 'Carregando o filme...',
    waiting: 'Carregando...',
    ended: 'Fim',
    error: 'Erro',
    ready: playing ? 'Reproduzindo' : 'Pausado',
  }[status]

  return (
    <div
      ref={containerRef}
      className={fullscreen ? 'video-player is-fullscreen' : 'video-player'}
      onKeyDown={handleKeyDown}
    >
      <div className="video-player-screen">
        <video
          ref={videoRef}
          className="video-player-media"
          src={movie.videoUrl}
          preload="metadata"
          autoPlay
          playsInline
          onClick={togglePlay}
          onLoadedMetadata={(event) => {
            setDuration(event.currentTarget.duration)
            setStatus('ready')
          }}
          onDurationChange={(event) => setDuration(event.currentTarget.duration)}
          onTimeUpdate={(event) => setCurrentTime(event.currentTarget.currentTime)}
          onPlay={() => setPlaying(true)}
          onPause={() => setPlaying(false)}
          onWaiting={() => setStatus('waiting')}
          onPlaying={() => setStatus('ready')}
          onEnded={() => {
            setPlaying(false)
            setStatus('ended')
          }}
          onVolumeChange={(event) => {
            setVolume(event.currentTarget.volume)
            setMuted(event.currentTarget.muted)
          }}
          onError={() => {
            setPlaying(false)
            setStatus('error')
          }}
        >
          Seu navegador não consegue tocar este vídeo.
        </video>

        {status === 'error' ? (
          <div className="video-player-message" role="alert">
            <p className="video-player-title">{movie.title}</p>
            <p>Não foi possível carregar o vídeo. O arquivo pode ter saído do ar ou estar num formato que o navegador não toca.</p>
            <a className="video-player-link" href={movie.videoUrl} target="_blank" rel="noopener noreferrer">
              Abrir o arquivo em outra aba
            </a>
          </div>
        ) : (
          !playing && (
            <button type="button" className="video-player-bigplay" onClick={togglePlay} aria-label="Reproduzir">
              {status === 'loading' || status === 'waiting' ? (
                <span className="video-player-spinner" aria-hidden="true" />
              ) : (
                <PixelIcon name="play" />
              )}
            </button>
          )
        )}
      </div>

      <div className="player-controls" role="group" aria-label="Controles do vídeo">
        <button
          type="button"
          className="player-button"
          onClick={togglePlay}
          aria-label={playing ? 'Pausar' : 'Reproduzir'}
          title={playing ? 'Pausar (K)' : 'Reproduzir (K)'}
          disabled={status === 'error'}
        >
          <PixelIcon name={playing ? 'pause' : 'play'} />
        </button>
        <button
          type="button"
          className="player-button"
          onClick={stop}
          aria-label="Parar e voltar ao início"
          title="Parar"
          disabled={status === 'error'}
        >
          <PixelIcon name="stop" />
        </button>

        <input
          type="range"
          className="player-seek"
          min={0}
          max={duration || 0}
          step={1}
          value={Math.min(currentTime, duration || 0)}
          style={{ '--progress': `${progress}%` }}
          onChange={(event) => seekTo(Number(event.target.value))}
          aria-label="Posição no filme"
          aria-valuetext={`${spokenTime(currentTime)} de ${spokenTime(duration)}`}
          disabled={!duration}
        />

        <span className="player-time" aria-hidden="true">
          {formatTime(currentTime)}
          <span className="player-time-total"> / {formatTime(duration)}</span>
        </span>

        <span className="player-volume">
          <button
            type="button"
            className="player-button"
            onClick={toggleMute}
            aria-label={muted ? 'Ligar o som' : 'Tirar o som'}
            title={muted ? 'Ligar o som (M)' : 'Tirar o som (M)'}
          >
            <PixelIcon name={muted ? 'muted' : 'sound'} />
          </button>
          <input
            type="range"
            className="player-volume-slider"
            min={0}
            max={1}
            step={0.05}
            value={effectiveVolume}
            style={{ '--progress': `${effectiveVolume * 100}%` }}
            onChange={(event) => changeVolume(Number(event.target.value))}
            aria-label="Volume"
            aria-valuetext={`${Math.round(effectiveVolume * 100)}%`}
          />
        </span>

        <button
          type="button"
          className="player-button"
          onClick={toggleFullscreen}
          aria-label={fullscreen ? 'Sair da tela cheia' : 'Tela cheia'}
          title="Tela cheia (F)"
        >
          <PixelIcon name="fullscreen" />
        </button>
      </div>

      <div className="player-statusbar">
        <span className="statusbar-pane" role="status">
          {statusText}
        </span>
        <span className="statusbar-pane player-shortcuts">Espaço ou K: tocar/pausar · ←/→: 10 s · M: som · F: tela cheia</span>
      </div>
    </div>
  )
}
