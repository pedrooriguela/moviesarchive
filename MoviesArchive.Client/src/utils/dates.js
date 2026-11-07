// Datas no formato do <input type="date"> (AAAA-MM-DD), sem passar por fuso horário

export function todayIso() {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

// AAAA-MM-DD -> DD/MM/AAAA
export function formatDate(iso) {
  if (!iso) return ''
  const [year, month, day] = iso.split('-')
  return `${day}/${month}/${year}`
}

// Data e hora da API (ISO com fuso) -> DD/MM/AAAA no horário de quem está vendo
export function formatDateTime(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('pt-BR')
}
