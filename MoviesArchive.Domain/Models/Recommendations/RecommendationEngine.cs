using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Models.Recommendations;

// Recomendação por semelhança de conteúdo (gêneros e diretor), a partir do histórico,
// das notas e da watchlist da pessoa. Não depende de banco: recebe tudo pronto e só calcula.
// A explicação completa, com um exemplo calculado passo a passo, está em docs/recomendacoes.md.
//
// Resumo:
//   1. Cada filme com que a pessoa interagiu ganha um peso (assistiu, gostou, não gostou, watchlist).
//   2. O perfil soma esses pesos por gênero e por diretor, e divide pelo maior valor (fica entre -1 e 1).
//   3. Candidatos: filmes que a pessoa ainda não assistiu, não avaliou e não guardou.
//   4. Pontuação = média do perfil nos gêneros do filme + bônus do diretor + ajuste pela nota da comunidade.
//   5. Ficam os de maior pontuação, cada um com o filme do histórico que mais pesou como motivo.
public static class RecommendationEngine
{
    // ---------- 1. Peso de cada sinal ----------
    public const double WatchedWeight = 1.0;     // assistiu (deu play pelo menos uma vez)
    public const double RewatchBonus = 0.25;     // cada play a mais...
    public const int MaxRewatchBonuses = 2;      // ...até 2 vezes: assistir de novo soma pouco
    public const double LikedWeight = 2.0;       // deu nota de 4 para cima
    public const double DislikedWeight = -1.0;   // deu nota de 2 para baixo
    public const double WatchlistWeight = 0.5;   // guardou na watchlist e ainda não assistiu
    public const decimal LikedFrom = 4m;
    public const decimal DislikedUpTo = 2m;

    // ---------- 4. Pontuação dos candidatos ----------
    public const double DirectorFactor = 0.5;    // peso do diretor em relação aos gêneros
    public const double CommunityFactor = 0.2;   // a nota da comunidade só desempata, nunca decide
    public const double PriorRating = 3.0;       // média "neutra" usada na média bayesiana
    public const int PriorVotes = 3;             // quantos votos neutros a média bayesiana finge ter

    public static List<Recommendation> Recommend(
        IReadOnlyCollection<Movie> catalog,
        ViewerSignals signals,
        IReadOnlyDictionary<Guid, MovieRatingStats> community,
        int limit)
    {
        // 1. Filmes que dizem algo sobre o gosto da pessoa, com o peso de cada um
        var sources = catalog
            .Select(movie => (Movie: movie, Weight: SignalWeight(movie.Id, signals)))
            .Where(source => source.Weight != 0)
            .ToList();

        // 2. Perfil: soma dos pesos por gênero e por diretor
        var genreAffinity = new Dictionary<string, double>();
        var directorAffinity = new Dictionary<string, double>();
        foreach (var (movie, weight) in sources)
        {
            foreach (var genre in movie.Genres)
                genreAffinity[genre] = genreAffinity.GetValueOrDefault(genre) + weight;

            if (DirectorKey(movie) is string director)
                directorAffinity[director] = directorAffinity.GetValueOrDefault(director) + weight;
        }

        var genreProfile = Normalize(genreAffinity);
        var directorProfile = Normalize(directorAffinity);

        // Sem nenhum gosto positivo conhecido, não há o que recomendar (a prateleira não aparece)
        if (!genreProfile.Values.Any(value => value > 0) && !directorProfile.Values.Any(value => value > 0))
            return [];

        // 3 e 4. Candidatos e pontuação
        var scored = new List<(Movie Movie, double Score, double Community)>();
        foreach (var movie in catalog.Where(candidate => !signals.HasInteracted(candidate.Id)))
        {
            var relevance = GenreScore(movie, genreProfile) + DirectorScore(movie, directorProfile);

            // Nada em comum com o gosto da pessoa: a nota da comunidade sozinha não basta
            if (relevance <= 0)
                continue;

            var communityScore = CommunityScore(community.GetValueOrDefault(movie.Id));
            scored.Add((movie, relevance + communityScore, communityScore));
        }

        // 5. Os melhores, com o motivo
        return scored
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Community)
            .ThenBy(item => item.Movie.Title, StringComparer.Ordinal)
            .Take(limit)
            .Select(item => new Recommendation(item.Movie, Math.Round(item.Score, 3), Reason(item.Movie, sources, signals)))
            .ToList();
    }

    // Peso de um filme para o perfil da pessoa (0 = não diz nada sobre o gosto dela)
    public static double SignalWeight(Guid movieId, ViewerSignals signals)
    {
        var weight = 0.0;

        var plays = signals.WatchCounts.GetValueOrDefault(movieId);
        if (plays > 0)
            weight += WatchedWeight + RewatchBonus * Math.Min(plays - 1, MaxRewatchBonuses);

        if (signals.Ratings.TryGetValue(movieId, out var rating))
        {
            if (rating >= LikedFrom)
                weight += LikedWeight;
            else if (rating <= DislikedUpTo)
                weight += DislikedWeight;
        }

        if (plays == 0 && signals.Watchlist.Contains(movieId))
            weight += WatchlistWeight;

        return weight;
    }

    // Divide pelo maior valor positivo: o gênero (ou diretor) preferido vale 1 e os demais, uma fração.
    // Valores negativos (não gostou) ficam limitados a -1.
    private static Dictionary<string, double> Normalize(Dictionary<string, double> affinity)
    {
        var max = affinity.Count == 0 ? 0 : affinity.Values.Max();
        if (max <= 0)
            return affinity;

        return affinity.ToDictionary(pair => pair.Key, pair => Math.Clamp(pair.Value / max, -1, 1));
    }

    // Média do perfil nos gêneros do candidato (gênero fora do perfil conta 0)
    private static double GenreScore(Movie movie, Dictionary<string, double> profile) =>
        movie.Genres.Count == 0 ? 0 : movie.Genres.Average(genre => profile.GetValueOrDefault(genre));

    // Bônus só para diretor de que a pessoa gosta; diretor de que não gosta não tira pontos
    private static double DirectorScore(Movie movie, Dictionary<string, double> profile) =>
        DirectorKey(movie) is string director && profile.TryGetValue(director, out var affinity) && affinity > 0
            ? DirectorFactor * affinity
            : 0;

    // Média bayesiana: poucos votos ficam perto de 3, então uma única nota 5 não domina.
    // Resultado entre -0,25 e +0,2; filme sem avaliações fica neutro (0).
    private static double CommunityScore(MovieRatingStats? stats)
    {
        if (stats == null || stats.ReviewCount == 0)
            return 0;

        var average = (double)stats.AverageRating;
        var bayesian = (PriorRating * PriorVotes + average * stats.ReviewCount) / (PriorVotes + stats.ReviewCount);
        return CommunityFactor * (bayesian - PriorRating) / 2;
    }

    // O filme do histórico que mais pesou na recomendação vira o motivo mostrado no cartão
    private static string Reason(Movie candidate, List<(Movie Movie, double Weight)> sources, ViewerSignals signals)
    {
        var best = sources
            .Where(source => source.Weight > 0)
            .Select(source => (source.Movie, Contribution: source.Weight * Similarity(candidate, source.Movie)))
            .Where(source => source.Contribution > 0)
            .OrderByDescending(source => source.Contribution)
            .ThenBy(source => source.Movie.Title, StringComparer.Ordinal)
            .Select(source => source.Movie)
            .FirstOrDefault();

        if (best == null)
            return "Com base no seu histórico";

        if (signals.Ratings.TryGetValue(best.Id, out var rating) && rating >= LikedFrom)
            return $"Porque você gostou de {best.Title}";

        if (signals.WatchCounts.ContainsKey(best.Id))
            return $"Porque você assistiu {best.Title}";

        return $"Parecido com {best.Title}, da sua watchlist";
    }

    // Quanto o candidato se parece com outro filme: fração dos gêneros dele que o outro também tem,
    // mais o fator do diretor quando é o mesmo
    private static double Similarity(Movie candidate, Movie other)
    {
        var sharedGenres = candidate.Genres.Count == 0
            ? 0
            : candidate.Genres.Count(genre => other.Genres.Contains(genre)) / (double)candidate.Genres.Count;

        var sameDirector = DirectorKey(candidate) is string director && director == DirectorKey(other)
            ? DirectorFactor
            : 0;

        return sharedGenres + sameDirector;
    }

    // Diretor sem diferença de maiúsculas nem espaços nas pontas
    private static string? DirectorKey(Movie movie) =>
        string.IsNullOrWhiteSpace(movie.Director) ? null : movie.Director.Trim().ToLowerInvariant();
}
