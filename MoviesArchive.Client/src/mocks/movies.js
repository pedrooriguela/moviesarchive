// Dados fictícios para desenvolver o catálogo antes de o banco ser atualizado.
// Os campos seguem a tabela Movies (Title, Description, ReleaseDate, Rating, Language,
// PosterUrl, Director) e as tabelas Genres / MovieGenres. Votes vem da contagem de Reviews.
//
// Contratos (espelham o back-end):
//   GET /api/movies/catalog?query&language&yearFrom&yearTo&sort&page&pageSize
//   GET /api/movies/home   -> { featured: MovieDetail | null, shelves: [{ id, title, movies }] }
//   GET /api/movies/{id}   -> MovieDetail
//   MovieCard:   { id, title, releaseYear, language, director, genres, rating, posterUrl }
//   MovieDetail: MovieCard + { votes, synopsis }   (synopsis = Movies.Description)
//
// Para testar o estado de erro na busca, pesquise por "erro".

export const MOVIES = [
  { id: 'm01', title: 'Metropolis', releaseYear: 1927, language: 'Alemão', director: 'Fritz Lang', genres: ['Drama', 'Ficção científica'], rating: 4.6, votes: 3120, synopsis: 'Em uma cidade dividida entre elite e operários, um jovem descobre o preço do luxo de seus governantes.' },
  { id: 'm02', title: 'Nosferatu', releaseYear: 1922, language: 'Alemão', director: 'F. W. Murnau', genres: ['Terror'], rating: 4.2, votes: 1840, synopsis: 'Um corretor de imóveis viaja aos Cárpatos e encontra um conde interessado em se mudar para a cidade dele.' },
  { id: 'm03', title: 'O Gabinete do Dr. Caligari', releaseYear: 1920, language: 'Alemão', director: 'Robert Wiene', genres: ['Suspense', 'Terror'], rating: 4.1, votes: 1275, synopsis: 'Em uma feira alemã, um hipnotizador usa um sonâmbulo para cometer crimes.' },
  { id: 'm04', title: 'Viagem à Lua', releaseYear: 1902, language: 'Francês', director: 'Georges Méliès', genres: ['Fantasia', 'Ficção científica'], rating: 3.8, votes: 512, synopsis: 'Um grupo de astrônomos parte num projétil em direção à Lua.' },
  { id: 'm05', title: 'Casablanca', releaseYear: 1942, language: 'Inglês', director: 'Michael Curtiz', genres: ['Drama', 'Romance'], rating: 4.5, votes: 4210, synopsis: 'Em Marrocos, durante a guerra, um dono de bar precisa escolher entre o amor e o dever quando um antigo caso reaparece.' },
  { id: 'm06', title: 'Cidadão Kane', releaseYear: 1941, language: 'Inglês', director: 'Orson Welles', genres: ['Drama'], rating: 4.4, votes: 3890, synopsis: 'A vida de um magnata da imprensa é reconstruída por meio das lembranças de quem o conheceu.' },
  { id: 'm07', title: 'Os Sete Samurais', releaseYear: 1954, language: 'Japonês', director: 'Akira Kurosawa', genres: ['Ação', 'Drama'], rating: 4.6, votes: 2760, synopsis: 'Um vilarejo contrata samurais sem mestre para proteger a colheita de bandidos.' },
  { id: 'm08', title: 'Viagem a Tóquio', releaseYear: 1953, language: 'Japonês', director: 'Yasujirō Ozu', genres: ['Drama'], rating: 4.3, votes: 980, synopsis: 'Um casal idoso visita os filhos em Tóquio e se sente deslocado na vida agitada da cidade.' },
  { id: 'm09', title: 'Psicose', releaseYear: 1960, language: 'Inglês', director: 'Alfred Hitchcock', genres: ['Suspense', 'Terror'], rating: 4.4, votes: 4520, synopsis: 'Uma secretária foge com dinheiro roubado e para num motel isolado administrado por um rapaz tímido.' },
  { id: 'm10', title: 'A Noite dos Mortos-Vivos', releaseYear: 1968, language: 'Inglês', director: 'George A. Romero', genres: ['Terror'], rating: 4.0, votes: 1630, synopsis: 'Um grupo se refugia numa casa isolada enquanto mortos-vivos cercam o lugar.' },
  { id: 'm11', title: 'Cinema Paradiso', releaseYear: 1988, language: 'Italiano', director: 'Giuseppe Tornatore', genres: ['Drama', 'Romance'], rating: 4.5, votes: 3350, synopsis: 'Um cineasta relembra a infância numa cidade pequena da Sicília e o projecionista que foi seu mentor.' },
  { id: 'm12', title: 'Central do Brasil', releaseYear: 1998, language: 'Português', director: 'Walter Salles', genres: ['Drama'], rating: 4.4, votes: 2870, synopsis: 'Uma ex-professora que escreve cartas para analfabetos se envolve com um menino órfão na estação Central do Rio.' },
  { id: 'm13', title: 'Pixote: A Lei do Mais Fraco', releaseYear: 1980, language: 'Português', director: 'Hector Babenco', genres: ['Crime', 'Drama'], rating: 4.1, votes: 1190, synopsis: 'Um menino abandonado num reformatório sobrevive nas ruas e se envolve com o crime.' },
  { id: 'm14', title: 'Oldboy', releaseYear: 2003, language: 'Coreano', director: 'Park Chan-wook', genres: ['Ação', 'Suspense'], rating: 4.3, votes: 3960, synopsis: 'Um homem é preso sem explicação por quinze anos e, ao ser libertado, parte em busca de vingança.' },
  { id: 'm15', title: 'Parasita', releaseYear: 2019, language: 'Coreano', director: 'Bong Joon-ho', genres: ['Drama', 'Suspense'], rating: 4.5, votes: 3640, synopsis: 'Uma família pobre se infiltra, um a um, na casa de uma família rica.' },
  { id: 'm16', title: 'Amélie', releaseYear: 2001, language: 'Francês', director: 'Jean-Pierre Jeunet', genres: ['Comédia', 'Romance'], rating: 4.2, votes: 3080, synopsis: 'Uma jovem de Montmartre decide mudar a vida das pessoas ao seu redor, em segredo.' },
  { id: 'm17', title: 'Pulp Fiction', releaseYear: 1994, language: 'Inglês', director: 'Quentin Tarantino', genres: ['Crime', 'Drama'], rating: 4.5, votes: 5210, synopsis: 'Histórias de criminosos de Los Angeles se entrelaçam ao longo de um fim de semana violento.' },
  { id: 'm18', title: 'O Iluminado', releaseYear: 1980, language: 'Inglês', director: 'Stanley Kubrick', genres: ['Suspense', 'Terror'], rating: 4.2, votes: 4370, synopsis: 'Um escritor aceita cuidar de um hotel isolado no inverno, e a solidão começa a afetá-lo.' },
  { id: 'm19', title: 'Um Corpo que Cai', releaseYear: 1958, language: 'Inglês', director: 'Alfred Hitchcock', genres: ['Romance', 'Suspense'], rating: 4.4, votes: 3090, synopsis: 'Um ex-detetive obcecado por uma mulher passa a investigar o passado dela.' },
  { id: 'm20', title: 'Quero Ser John Malkovich', releaseYear: 1999, language: 'Inglês', director: 'Spike Jonze', genres: ['Comédia', 'Fantasia'], rating: 3.9, votes: 2410, synopsis: 'Um escriturário descobre uma passagem que leva à mente do ator John Malkovich.' },
]

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
        items: sorted.slice(start, start + pageSize),
        page,
        pageSize,
        totalCount,
        totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
      })
    }, 500)
  })
}

function rankedByRating() {
  return MOVIES.filter((movie) => movie.rating != null).sort(
    (a, b) => b.rating - a.rating || a.title.localeCompare(b.title, 'pt-BR'),
  )
}

export function mockGetHome() {
  return new Promise((resolve) => {
    setTimeout(() => {
      const ranked = rankedByRating()
      const genres = [...new Set(MOVIES.flatMap((movie) => movie.genres))].sort((a, b) =>
        a.localeCompare(b, 'pt-BR'),
      )

      const shelves = [{ id: 'mais-bem-avaliados', title: 'Mais bem avaliados', movies: ranked.slice(0, 10) }]
      for (const genre of genres) {
        const movies = ranked.filter((movie) => movie.genres.includes(genre)).slice(0, 10)
        if (movies.length === 0) continue
        shelves.push({ id: `genero-${genre.toLowerCase().replace(/ /g, '-')}`, title: genre, movies })
      }

      resolve({ featured: ranked[0] ?? null, shelves })
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
      resolve({ ...movie })
    }, 400)
  })
}
