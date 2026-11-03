import MovieArt from '../components/MovieArt.jsx'
import Shelf from '../components/Shelf.jsx'

// Vitrine fixa da entrada: dados de apresentação, não do acervo.
// Não vem da API, então nada do banco fica exposto a quem não tem conta.
const VITRINE = [
  { id: 'vitrine-1', title: 'Metropolis', releaseYear: 1927, director: 'Fritz Lang', language: 'Alemão', rating: null, votes: 0 },
  { id: 'vitrine-2', title: 'Casablanca', releaseYear: 1942, director: 'Michael Curtiz', language: 'Inglês', rating: null, votes: 0 },
  { id: 'vitrine-3', title: 'Psicose', releaseYear: 1960, director: 'Alfred Hitchcock', language: 'Inglês', rating: null, votes: 0 },
  { id: 'vitrine-4', title: 'Os Sete Samurais', releaseYear: 1954, director: 'Akira Kurosawa', language: 'Japonês', rating: null, votes: 0 },
  { id: 'vitrine-5', title: 'Parasita', releaseYear: 2019, director: 'Bong Joon-ho', language: 'Coreano', rating: null, votes: 0 },
  { id: 'vitrine-6', title: 'Acossado', releaseYear: 1960, director: 'Jean-Luc Godard', language: 'Francês', rating: null, votes: 0 },
]

// Sequência real de uso: cadastro, acervo e listas
const STEPS = [
  { title: 'Crie sua conta', text: 'Escolha um nome de usuário, seu e-mail e uma senha.' },
  { title: 'Monte seu acervo', text: 'Busque por título, diretor ou gênero e abra a ficha de cada filme.' },
  { title: 'Organize suas listas', text: 'Crie listas e escolha quais ficam públicas e quais ficam privadas.' },
]

export default function LandingPage({ notice, onSignUp, onLogin }) {
  const hero = VITRINE[0]

  return (
    <div className="landing">
      {notice && (
        <div className="alert alert-success" role="status">
          {notice}
        </div>
      )}

      <section className="landing-hero" aria-labelledby="landing-titulo">
        <div className="landing-hero-art" aria-hidden="true">
          <MovieArt movie={hero} variant="backdrop" />
        </div>
        <div className="landing-hero-content">
          <h1 id="landing-titulo" className="landing-title">
            Seu acervo de cinema, em um lugar só.
          </h1>
          <p className="landing-lead">
            Filmes clássicos e modernos reunidos em fichas com sinopse, nota e ficha técnica.
            Busque, organize e guarde os seus.
          </p>
          <div className="landing-actions">
            <button type="button" className="btn-primary landing-cta" onClick={() => onSignUp()}>
              Criar minha conta
            </button>
            <button type="button" className="btn-secondary landing-cta" onClick={onLogin}>
              Já tenho conta
            </button>
          </div>
        </div>
      </section>

      <Shelf
        id="vitrine"
        title="Em destaque no acervo"
        movies={VITRINE}
        onOpen={(id) => onSignUp(VITRINE.find((movie) => movie.id === id)?.title)}
      />

      <section className="landing-steps" aria-labelledby="passos-titulo">
        <h2 id="passos-titulo" className="shelf-title">
          Como funciona
        </h2>
        <ol className="steps">
          {STEPS.map((step) => (
            <li key={step.title} className="step">
              <h3>{step.title}</h3>
              <p>{step.text}</p>
            </li>
          ))}
        </ol>
      </section>

      <p className="landing-soon">Em breve: avaliações e perfis de outros espectadores.</p>
    </div>
  )
}
