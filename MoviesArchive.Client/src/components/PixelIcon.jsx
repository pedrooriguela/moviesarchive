// Ícones de 16x16 "pixels", desenhados com retângulos, no estilo dos ícones do Windows 95.
// Cada desenho é uma lista de [x, y, largura, altura, cor].
const INK = '#000000'
const AMBER = '#f2b630'
const NAVY = '#000080'
const WHITE = '#ffffff'
const GRAY = '#808080'
const RED = '#800000'

const DRAWINGS = {
  // Fita VHS com etiqueta: as listas
  lists: [
    [1, 3, 14, 10, INK],
    [3, 5, 10, 3, WHITE],
    [4, 6, 6, 1, NAVY],
    [3, 9, 3, 3, GRAY],
    [10, 9, 3, 3, GRAY],
    [4, 10, 1, 1, INK],
    [11, 10, 1, 1, INK],
  ],
  // Relógio: o histórico
  history: [
    [5, 1, 6, 1, INK],
    [3, 2, 2, 1, INK],
    [11, 2, 2, 1, INK],
    [2, 3, 1, 2, INK],
    [13, 3, 1, 2, INK],
    [1, 5, 1, 6, INK],
    [14, 5, 1, 6, INK],
    [2, 11, 1, 2, INK],
    [13, 11, 1, 2, INK],
    [3, 13, 2, 1, INK],
    [11, 13, 2, 1, INK],
    [5, 14, 6, 1, INK],
    [3, 3, 10, 10, WHITE],
    [2, 5, 1, 6, WHITE],
    [13, 5, 1, 6, WHITE],
    [5, 2, 6, 1, WHITE],
    [5, 13, 6, 1, WHITE],
    [7, 4, 2, 5, NAVY],
    [9, 7, 3, 2, NAVY],
  ],
  // Ingresso de cinema: a assinatura
  subscription: [
    [1, 4, 14, 8, AMBER],
    [1, 4, 14, 1, INK],
    [1, 11, 14, 1, INK],
    [1, 4, 1, 8, INK],
    [14, 4, 1, 8, INK],
    [0, 7, 2, 2, INK],
    [14, 7, 2, 2, INK],
    [10, 5, 1, 1, INK],
    [10, 7, 1, 1, INK],
    [10, 9, 1, 1, INK],
    [3, 6, 5, 1, INK],
    [3, 8, 4, 1, INK],
  ],
  // Claquete: a administração do catálogo
  admin: [
    [1, 6, 14, 9, INK],
    [2, 7, 12, 7, GRAY],
    [3, 9, 10, 1, WHITE],
    [3, 11, 7, 1, WHITE],
    [1, 2, 14, 3, INK],
    [3, 2, 2, 3, WHITE],
    [7, 2, 2, 3, WHITE],
    [11, 2, 2, 3, WHITE],
    [1, 5, 14, 1, AMBER],
  ],
  // Porta com seta: sair
  logout: [
    [2, 1, 8, 14, INK],
    [3, 2, 6, 12, GRAY],
    [7, 8, 1, 1, AMBER],
    [9, 7, 5, 2, RED],
    [12, 5, 1, 6, RED],
    [13, 6, 1, 4, RED],
    [14, 7, 1, 2, RED],
  ],
}

export default function PixelIcon({ name, size = 32 }) {
  const drawing = DRAWINGS[name]
  if (!drawing) return null

  return (
    <svg
      className="pixel-art"
      width={size}
      height={size}
      viewBox="0 0 16 16"
      shapeRendering="crispEdges"
      aria-hidden="true"
      focusable="false"
    >
      {drawing.map(([x, y, width, height, fill], index) => (
        <rect key={index} x={x} y={y} width={width} height={height} fill={fill} />
      ))}
    </svg>
  )
}
