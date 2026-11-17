// Formatação de números e datas do painel de estatísticas (em português)

const numberFormat = new Intl.NumberFormat('pt-BR')
const dayFormat = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', timeZone: 'UTC' })
const longDayFormat = new Intl.DateTimeFormat('pt-BR', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' })
const monthFormat = new Intl.DateTimeFormat('pt-BR', { month: 'short', year: '2-digit', timeZone: 'UTC' })
const longMonthFormat = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric', timeZone: 'UTC' })

export function formatNumber(value) {
  return numberFormat.format(value)
}

export function plural(count, one, many) {
  return `${formatNumber(count)} ${count === 1 ? one : many}`
}

// A API manda a data como "2026-11-17"; em UTC ela não muda de dia em nenhum fuso
function parseDate(value) {
  return new Date(`${value}T00:00:00Z`)
}

export function formatPointDate(value, unit, long = false) {
  const date = parseDate(value)
  if (unit === 'month') return (long ? longMonthFormat : monthFormat).format(date).replace('.', '')
  return (long ? longDayFormat : dayFormat).format(date)
}
