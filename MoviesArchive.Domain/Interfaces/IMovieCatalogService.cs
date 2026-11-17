using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IMovieCatalogService
{
    Task<PagedResponseDto<MovieCardDto>> SearchAsync(MovieSearchFilter filter);
    Task<MovieDetailDto> GetDetailAsync(Guid movieId);
    Task<MovieHomeDto> GetHomeAsync();

    // Cartões prontos (média das avaliações e marca de visto), usados também pela watchlist
    Task<List<MovieCardDto>> ToCardsAsync(IReadOnlyCollection<Movie> movies);
}
