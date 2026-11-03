using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies.Dtos;
using MoviesArchive.Domain.Models.Watchlist;

namespace MoviesArchive.Infra.Services;

public class WatchlistService : IWatchlistService
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly MovieCatalogService _movieCatalogService;

    public WatchlistService(
        IWatchlistRepository watchlistRepository,
        IMovieRepository movieRepository)
    {
        _watchlistRepository = watchlistRepository;
        _movieRepository = movieRepository;
        _movieCatalogService = new MovieCatalogService(movieRepository);
    }

    // Mesmos cartões do catálogo, com a média das avaliações
    public async Task<List<MovieCardDto>> GetMoviesAsync(Guid userId)
    {
        var movies = await _watchlistRepository.GetMoviesAsync(userId);
        return await _movieCatalogService.ToCardsAsync(movies);
    }

    // Idempotente: adicionar um filme que já está na watchlist não dá erro
    public async Task AddAsync(Guid userId, Guid movieId)
    {
        if (await _movieRepository.GetByIdAsync(movieId) == null)
            throw new MovieNotFoundException();

        if (await _watchlistRepository.ContainsAsync(userId, movieId))
            return;

        await _watchlistRepository.AddAsync(new WatchlistItem(userId, movieId));
    }

    // Idempotente: remover um filme que não está na watchlist não dá erro
    public async Task RemoveAsync(Guid userId, Guid movieId)
    {
        var item = await _watchlistRepository.GetAsync(userId, movieId);
        if (item != null)
            await _watchlistRepository.RemoveAsync(item);
    }
}
