// Classificação indicativa: idade mínima gravada em Movies.AgeRating (mesma lista de
// AgeRatings.cs no back-end). 0 = livre.
export const AGE_RATINGS = [
  { value: 0, label: 'Livre' },
  { value: 10, label: '10 anos' },
  { value: 12, label: '12 anos' },
  { value: 14, label: '14 anos' },
  { value: 16, label: '16 anos' },
  { value: 18, label: '18 anos' },
]

export function ageRatingLabel(value) {
  return value === 0 ? 'L' : String(value)
}
