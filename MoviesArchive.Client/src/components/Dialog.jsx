import { useEffect } from 'react'

// Janela modal no estilo Windows 95: barra de título azul e botão de fechar
export default function Dialog({ title, onClose, children }) {
  useEffect(() => {
    function handleKeyDown(event) {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  return (
    <div
      className="dialog-overlay"
      onMouseDown={(event) => {
        // Clique fora da janela fecha; clique dentro não
        if (event.target === event.currentTarget) onClose()
      }}
    >
      <section className="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-titulo">
        <div className="dialog-title">
          <h2 id="dialog-titulo">{title}</h2>
          <button type="button" className="dialog-close" onClick={onClose} aria-label="Fechar">
            ×
          </button>
        </div>
        <div className="dialog-body">{children}</div>
      </section>
    </div>
  )
}
