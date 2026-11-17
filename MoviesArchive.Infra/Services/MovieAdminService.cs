using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Admin;
using MoviesArchive.Domain.Models.Admin.Dtos;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Infra.Services;

// Catálogo do painel do administrador. Remover não apaga a linha: o filme ganha DeletedAt,
// some para os usuários (filtro global Removed) e continua no histórico de quem já assistiu.
public class MovieAdminService : IMovieAdminService
{
    private const int MinYear = 1880;
    private const int MaxYear = 2100;
    private const int MaxRuntimeMinutes = 1000;

    private readonly IMovieRepository _movieRepository;

    public MovieAdminService(
        IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<PagedResponseDto<AdminMovieDto>> SearchAsync(string? query, AdminMovieStatus status, int page, int pageSize)
    {
        var (items, totalCount) = await _movieRepository.SearchForAdminAsync(query, status, page, pageSize);
        var accesses = await _movieRepository.CountAccessesAsync(items.Select(movie => movie.Id).ToList());

        return new PagedResponseDto<AdminMovieDto>
        {
            Items = items.Select(movie => ToDto(movie, accesses.GetValueOrDefault(movie.Id))).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)),
        };
    }

    public async Task<AdminMovieDto> GetAsync(Guid movieId)
    {
        var movie = await GetMovieAsync(movieId);
        return await ToDtoAsync(movie);
    }

    public async Task<AdminMovieDto> CreateAsync(SaveMovieRequestDto request)
    {
        var data = Validate(request);

        if (await _movieRepository.ExistsInCatalogAsync(data.Title, data.ReleaseDate?.Year))
            throw new MovieAlreadyExistsException();

        var movie = Movie.Builder.Create(data.Title, data.ReleaseDate);
        Apply(movie, data);

        await _movieRepository.AddAsync(movie);

        return ToDto(movie, accessCount: 0);
    }

    public async Task<AdminMovieDto> UpdateAsync(Guid movieId, SaveMovieRequestDto request)
    {
        var movie = await GetMovieAsync(movieId);
        var data = Validate(request);

        if (!movie.IsRemoved &&
            await _movieRepository.ExistsInCatalogAsync(data.Title, data.ReleaseDate?.Year, movie.Id))
            throw new MovieAlreadyExistsException();

        Apply(movie, data);
        await _movieRepository.SaveChangesAsync();

        return await ToDtoAsync(movie);
    }

    // Idempotente: remover um filme já removido mantém a data da primeira remoção
    public async Task<AdminMovieDto> RemoveAsync(Guid movieId)
    {
        var movie = await GetMovieAsync(movieId);

        movie.Remove();
        await _movieRepository.SaveChangesAsync();

        return await ToDtoAsync(movie);
    }

    // Volta para o catálogo e reaparece nas listas e watchlists em que estava
    public async Task<AdminMovieDto> RestoreAsync(Guid movieId)
    {
        var movie = await GetMovieAsync(movieId);

        if (movie.IsRemoved &&
            await _movieRepository.ExistsInCatalogAsync(movie.Title, movie.ReleaseDate?.Year, movie.Id))
            throw new MovieAlreadyExistsException();

        movie.Restore();
        await _movieRepository.SaveChangesAsync();

        return await ToDtoAsync(movie);
    }

    private async Task<Movie> GetMovieAsync(Guid movieId) =>
        await _movieRepository.GetForAdminAsync(movieId)
        ?? throw new MovieNotFoundException();

    private record MovieData(
        string Title,
        DateOnly? ReleaseDate,
        string? Description,
        string? Language,
        string? Director,
        string? PosterUrl,
        List<string> Genres,
        int AgeRating,
        int? RuntimeMinutes,
        string? VideoUrl);

    // Confere e normaliza os campos; o nome do campo na exceção é o mesmo do formulário do front
    private static MovieData Validate(SaveMovieRequestDto request)
    {
        var title = request.Title.Trim();
        if (title.Length == 0)
            throw new InvalidMovieDataException("title", "O título é obrigatório.");

        if (request.ReleaseDate is DateOnly releaseDate && (releaseDate.Year < MinYear || releaseDate.Year > MaxYear))
            throw new InvalidMovieDataException("releaseDate", $"Informe uma data entre {MinYear} e {MaxYear}.");

        var validGenres = Enum.GetNames<Genre>();
        var genres = request.Genres
            .Select(code => code.Trim())
            .Where(code => code.Length > 0)
            .Distinct()
            .ToList();

        if (genres.Count == 0)
            throw new InvalidMovieDataException("genres", "Escolha pelo menos um gênero.");

        if (genres.Any(code => !validGenres.Contains(code)))
            throw new InvalidMovieDataException("genres", "Gênero inválido.");

        if (request.AgeRating is not int ageRating || !AgeRatings.IsValid(ageRating))
            throw new InvalidMovieDataException("ageRating", "Escolha a classificação indicativa.");

        if (request.RuntimeMinutes is int runtime && (runtime <= 0 || runtime > MaxRuntimeMinutes))
            throw new InvalidMovieDataException("runtimeMinutes", $"A duração deve ficar entre 1 e {MaxRuntimeMinutes} minutos.");

        return new MovieData(
            title,
            request.ReleaseDate,
            Normalize(request.Description),
            Normalize(request.Language),
            Normalize(request.Director),
            NormalizeUrl(request.PosterUrl, "posterUrl", "O pôster deve ser um endereço que começa com http:// ou https://."),
            // Mesma ordem do enum, para o banco ficar organizado
            validGenres.Where(genres.Contains).ToList(),
            ageRating,
            request.RuntimeMinutes,
            NormalizeUrl(request.VideoUrl, "videoUrl", "O vídeo deve ser um endereço que começa com http:// ou https://."));
    }

    private static void Apply(Movie movie, MovieData data)
    {
        movie.Title = data.Title;
        movie.ReleaseDate = data.ReleaseDate;
        movie.Description = data.Description;
        movie.Language = data.Language;
        movie.Director = data.Director;
        movie.PosterUrl = data.PosterUrl;
        movie.Genres = data.Genres;
        movie.AgeRating = data.AgeRating;
        movie.RuntimeMinutes = data.RuntimeMinutes;
        movie.VideoUrl = data.VideoUrl;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeUrl(string? value, string field, string message)
    {
        var url = Normalize(value);
        if (url == null)
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidMovieDataException(field, message);

        return url;
    }

    private async Task<AdminMovieDto> ToDtoAsync(Movie movie)
    {
        var accesses = await _movieRepository.CountAccessesAsync([movie.Id]);
        return ToDto(movie, accesses.GetValueOrDefault(movie.Id));
    }

    private static AdminMovieDto ToDto(Movie movie, int accessCount) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        ReleaseDate = movie.ReleaseDate,
        ReleaseYear = movie.ReleaseDate?.Year,
        Description = movie.Description,
        Language = movie.Language,
        Director = movie.Director,
        PosterUrl = movie.PosterUrl,
        Genres = movie.Genres.ToList(),
        AgeRating = movie.AgeRating,
        RuntimeMinutes = movie.RuntimeMinutes,
        VideoUrl = movie.VideoUrl,
        IsRemoved = movie.IsRemoved,
        DeletedAt = movie.DeletedAt,
        AccessCount = accessCount,
    };
}
