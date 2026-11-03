// Barra de status no rodapé da janela, com painéis afundados
export default function StatusBar({ items }) {
  return (
    <div className="statusbar">
      {items.filter(Boolean).map((item) => (
        <span key={item} className="statusbar-pane">
          {item}
        </span>
      ))}
    </div>
  )
}
