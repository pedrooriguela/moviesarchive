// Dados fictícios para desenvolver o catálogo sem a API (VITE_USE_MOCKS diferente de 'false').
// Os campos seguem a tabela Movies (Title, Description, ReleaseDate, Language, PosterUrl, Director)
// e Genres. A nota (rating) e a quantidade (votes) não ficam aqui: vêm das avaliações fictícias
// (mocks/reviewStore.js), com as mesmas regras da API.
//
// Contratos (espelham o back-end):
//   GET /api/movies/catalog?query&language&yearFrom&yearTo&sort&page&pageSize
//   GET /api/movies/home   -> { featured: MovieDetail | null, shelves: [{ id, title, movies }] }
//   GET /api/movies/{id}   -> MovieDetail
//   MovieCard:   { id, title, releaseYear, language, director, genres, rating, posterUrl }
//   MovieDetail: MovieCard + { votes, synopsis }   (rating = média das avaliações, votes = quantidade)
//
// Para testar o estado de erro na busca, pesquise por "erro".
import { ratingStatsOf } from './reviewStore.js'

export const MOVIES = [
  { id: 'm01', title: 'Metropolis', releaseYear: 1927, language: 'Alemão', director: 'Fritz Lang', genres: ['Drama', 'Ficção científica'], synopsis: 'Em uma cidade dividida entre elite e operários, um jovem descobre o preço do luxo de seus governantes.' },
  { id: 'm02', title: 'Nosferatu', releaseYear: 1922, language: 'Alemão', director: 'F. W. Murnau', genres: ['Terror'], synopsis: 'Um corretor de imóveis viaja aos Cárpatos e encontra um conde interessado em se mudar para a cidade dele.' },
  { id: 'm03', title: 'O Gabinete do Dr. Caligari', releaseYear: 1920, language: 'Alemão', director: 'Robert Wiene', genres: ['Suspense', 'Terror'], synopsis: 'Em uma feira alemã, um hipnotizador usa um sonâmbulo para cometer crimes.' },
  { id: 'm04', title: 'Viagem à Lua', releaseYear: 1902, language: 'Francês', director: 'Georges Méliès', genres: ['Fantasia', 'Ficção científica'], synopsis: 'Um grupo de astrônomos parte num projétil em direção à Lua.' },
  { id: 'm05', title: 'Casablanca', releaseYear: 1942, language: 'Inglês', director: 'Michael Curtiz', genres: ['Drama', 'Romance'], synopsis: 'Em Marrocos, durante a guerra, um dono de bar precisa escolher entre o amor e o dever quando um antigo caso reaparece.' },
  { id: 'm06', title: 'Cidadão Kane', releaseYear: 1941, language: 'Inglês', director: 'Orson Welles', genres: ['Drama'], synopsis: 'A vida de um magnata da imprensa é reconstruída por meio das lembranças de quem o conheceu.' },
  { id: 'm07', title: 'Os Sete Samurais', releaseYear: 1954, language: 'Japonês', director: 'Akira Kurosawa', genres: ['Ação', 'Drama'], synopsis: 'Um vilarejo contrata samurais sem mestre para proteger a colheita de bandidos.' },
  { id: 'm08', title: 'Viagem a Tóquio', releaseYear: 1953, language: 'Japonês', director: 'Yasujirō Ozu', genres: ['Drama'], synopsis: 'Um casal idoso visita os filhos em Tóquio e se sente deslocado na vida agitada da cidade.' },
  { id: 'm09', title: 'Psicose', releaseYear: 1960, language: 'Inglês', director: 'Alfred Hitchcock', genres: ['Suspense', 'Terror'], synopsis: 'Uma secretária foge com dinheiro roubado e para num motel isolado administrado por um rapaz tímido.' },
  { id: 'm10', title: 'A Noite dos Mortos-Vivos', releaseYear: 1968, language: 'Inglês', director: 'George A. Romero', genres: ['Terror'], synopsis: 'Um grupo se refugia numa casa isolada enquanto mortos-vivos cercam o lugar.' },
  { id: 'm11', title: 'Cinema Paradiso', releaseYear: 1988, language: 'Italiano', director: 'Giuseppe Tornatore', genres: ['Drama', 'Romance'], synopsis: 'Um cineasta relembra a infância numa cidade pequena da Sicília e o projecionista que foi seu mentor.' },
  { id: 'm12', title: 'Central do Brasil', releaseYear: 1998, language: 'Português', director: 'Walter Salles', genres: ['Drama'], synopsis: 'Uma ex-professora que escreve cartas para analfabetos se envolve com um menino órfão na estação Central do Rio.' },
  { id: 'm13', title: 'Pixote: A Lei do Mais Fraco', releaseYear: 1980, language: 'Português', director: 'Hector Babenco', genres: ['Crime', 'Drama'], synopsis: 'Um menino abandonado num reformatório sobrevive nas ruas e se envolve com o crime.' },
  { id: 'm14', title: 'Oldboy', releaseYear: 2003, language: 'Coreano', director: 'Park Chan-wook', genres: ['Ação', 'Suspense'], synopsis: 'Um homem é preso sem explicação por quinze anos e, ao ser libertado, parte em busca de vingança.' },
  { id: 'm15', title: 'Parasita', releaseYear: 2019, language: 'Coreano', director: 'Bong Joon-ho', genres: ['Drama', 'Suspense'], synopsis: 'Uma família pobre se infiltra, um a um, na casa de uma família rica.' },
  { id: 'm16', title: 'Amélie', releaseYear: 2001, language: 'Francês', director: 'Jean-Pierre Jeunet', genres: ['Comédia', 'Romance'], synopsis: 'Uma jovem de Montmartre decide mudar a vida das pessoas ao seu redor, em segredo.' },
  { id: 'm17', title: 'Pulp Fiction', releaseYear: 1994, language: 'Inglês', director: 'Quentin Tarantino', genres: ['Crime', 'Drama'], synopsis: 'Histórias de criminosos de Los Angeles se entrelaçam ao longo de um fim de semana violento.' },
  { id: 'm18', title: 'O Iluminado', releaseYear: 1980, language: 'Inglês', director: 'Stanley Kubrick', genres: ['Suspense', 'Terror'], synopsis: 'Um escritor aceita cuidar de um hotel isolado no inverno, e a solidão começa a afetá-lo.' },
  { id: 'm19', title: 'Um Corpo que Cai', releaseYear: 1958, language: 'Inglês', director: 'Alfred Hitchcock', genres: ['Romance', 'Suspense'], synopsis: 'Um ex-detetive obcecado por uma mulher passa a investigar o passado dela.' },
  { id: 'm20', title: 'Quero Ser John Malkovich', releaseYear: 1999, language: 'Inglês', director: 'Spike Jonze', genres: ['Comédia', 'Fantasia'], synopsis: 'Um escriturário descobre uma passagem que leva à mente do ator John Malkovich.' },
]

// Ficha com rating = média das avaliações e votes = quantidade (null/0 sem avaliações)
export function withRating(movie) {
  const stats = ratingStatsOf(movie.id)
  return { ...movie, rating: stats?.averageRating ?? null, votes: stats?.reviewCount ?? 0 }
}

// Avaliados primeiro (maior média), depois os sem avaliação; empate pelo título
function byRating(a, b) {
  if ((a.rating == null) !== (b.rating == null)) return a.rating == null ? 1 : -1
  return (b.rating ?? 0) - (a.rating ?? 0) || a.title.localeCompare(b.title, 'pt-BR')
}

// Remove acentos e diferenças de maiúsculas para comparar textos
function normalize(text) {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()
}

function matchesTerm(movie, term) {
  const haystack = normalize([movie.title, movie.director ?? '', ...movie.genres].join(' '))
  return haystack.includes(term)
}

function sortMovies(items, sort) {
  const copy = [...items]
  if (sort === 'year-desc') return copy.sort((a, b) => b.releaseYear - a.releaseYear || a.title.localeCompare(b.title, 'pt-BR'))
  if (sort === 'year-asc') return copy.sort((a, b) => a.releaseYear - b.releaseYear || a.title.localeCompare(b.title, 'pt-BR'))
  return copy.sort((a, b) => a.title.localeCompare(b.title, 'pt-BR'))
}

export function mockSearchCatalog({
  query = '',
  language = '',
  yearFrom,
  yearTo,
  sort = 'default',
  page = 1,
  pageSize = 12,
} = {}) {
  return new Promise((resolve, reject) => {
    // Simula a latência de uma requisição real
    setTimeout(() => {
      if (query.trim().toLowerCase() === 'erro') {
        reject(new Error('Não foi possível buscar no acervo. Tente novamente.'))
        return
      }

      const term = normalize(query.trim())
      const filtered = MOVIES.filter((movie) => !term || matchesTerm(movie, term))
        .filter((movie) => !language || movie.language === language)
        .filter((movie) => yearFrom === undefined || movie.releaseYear >= yearFrom)
        .filter((movie) => yearTo === undefined || movie.releaseYear <= yearTo)

      const sorted = sortMovies(filtered, sort)
      const totalCount = sorted.length
      const start = (page - 1) * pageSize

      resolve({
        items: sorted.slice(start, start + pageSize).map(withRating),
        page,
        pageSize,
        totalCount,
        totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
      })
    }, 500)
  })
}

export function mockGetHome() {
  return new Promise((resolve) => {
    setTimeout(() => {
      const all = MOVIES.map(withRating).sort(byRating)
      // "Mais bem avaliados": só filmes com avaliações
      const topRated = all.filter((movie) => movie.rating != null).slice(0, 10)
      const genres = [...new Set(MOVIES.flatMap((movie) => movie.genres))].sort((a, b) =>
        a.localeCompare(b, 'pt-BR'),
      )

      const shelves = []
      if (topRated.length) shelves.push({ id: 'mais-bem-avaliados', title: 'Mais bem avaliados', movies: topRated })
      // Prateleiras de gênero: avaliados primeiro, depois os demais
      for (const genre of genres) {
        const movies = all.filter((movie) => movie.genres.includes(genre)).slice(0, 10)
        if (movies.length === 0) continue
        shelves.push({ id: `genero-${genre.toLowerCase().replace(/ /g, '-')}`, title: genre, movies })
      }

      resolve({ featured: topRated[0] ?? null, shelves })
    }, 500)
  })
}

export function mockGetMovie(id) {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      const movie = MOVIES.find((m) => m.id === id)
      if (!movie) {
        reject(new Error('Filme não encontrado no acervo.'))
        return
      }
      resolve(withRating(movie))
    }, 400)
  })
}
