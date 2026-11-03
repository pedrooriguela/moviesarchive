import { getPhotoSrc } from '../services/api.js'

const COLORS = ['#000080', '#800000', '#006400', '#5a3d00', '#3a3a3a', '#4b0082']

function colorFor(name) {
  let hash = 0
  for (const char of name) hash = (hash * 31 + char.charCodeAt(0)) >>> 0
  return COLORS[hash % COLORS.length]
}

// Foto do usuário ou a inicial do nome sobre uma cor fixa por nome
export default function Avatar({ name = '', photoUrl, size = 40 }) {
  const style = { width: size, height: size }
  if (photoUrl) return <img className="avatar" src={getPhotoSrc(photoUrl)} alt="" style={style} />

  return (
    <span className="avatar avatar-initial" style={{ ...style, background: colorFor(name) }} aria-hidden="true">
      {name.trim().charAt(0).toUpperCase() || '?'}
    </span>
  )
}
