using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Lists.Dtos;
using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Infra.Services;

// O botão "Adicionar à watchlist" da ficha. A watchlist é uma lista comum (criada sozinha na
// primeira vez), então adicionar e remover seguem as regras de MovieListService.
public class WatchlistService : IWatchlistService
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IMovieListRepository _movieListRepository;
    private readonly IMovieListService _movieListService;
    private readonly IMovieCatalogService _movieCatalogService;

    public WatchlistService(
        IWatchlistRepository watchlistRepository,
        IMovieListRepository movieListRepository,
        IMovieListService movieListService,
        IMovieCatalogService movieCatalogService)
    {
        _watchlistRepository = watchlistRepository;
        _movieListRepository = movieListRepository;
        _movieListService = movieListService;
        _movieCatalogService = movieCatalogService;
    }

    // Mesmos cartões do catálogo, com a média das avaliações, na ordem da lista
    public async Task<List<MovieCardDto>> GetMoviesAsync(Guid userId)
    {
        var movies = await _watchlistRepository.GetMoviesAsync(userId);
        return await _movieCatalogService.ToCardsAsync(movies);
    }

    // Idempotente: adicionar um filme que já está na watchlist não dá erro. Entra no fim da lista.
    public async Task AddAsync(Guid userId, Guid movieId)
    {
        var watchlist = await _movieListRepository.GetOrCreateWatchlistAsync(userId);

        try
        {
            await _movieListService.AddItemAsync(watchlist.Id, userId, new AddListItemRequestDto { MovieId = movieId });
        }
        catch (MovieAlreadyInListException)
        {
            // Já estava lá: o resultado desejado já existe
        }
    }

    // Idempotente: remover um filme que não está na watchlist não dá erro
    public async Task RemoveAsync(Guid userId, Guid movieId)
    {
        var watchlist = await _movieListRepository.GetOrCreateWatchlistAsync(userId);

        try
        {
            await _movieListService.RemoveItemAsync(watchlist.Id, userId, movieId);
        }
        catch (ListItemNotFoundException)
        {
            // Já tinha saído
        }
    }
}
