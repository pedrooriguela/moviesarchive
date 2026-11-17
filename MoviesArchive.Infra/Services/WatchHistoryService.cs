using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.History;
using MoviesArchive.Domain.Models.History.Dtos;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Infra.Services;

public class WatchHistoryService : IWatchHistoryService
{
    private readonly IWatchHistoryRepository _watchHistoryRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IUserRepository _userRepository;

    public WatchHistoryService(
        IWatchHistoryRepository watchHistoryRepository,
        IMovieRepository movieRepository,
        IUserRepository userRepository)
    {
        _watchHistoryRepository = watchHistoryRepository;
        _movieRepository = movieRepository;
        _userRepository = userRepository;
    }

    public async Task<WatchHistoryEntryResponseDto> RecordAsync(Guid userId, RecordWatchRequestDto request)
    {
        // Assistir é o que o plano libera: a conta gratuita navega, mas não dá play
        var user = await _userRepository.GetByIdAsync(userId);
        if (user?.SubscriptionId == null)
            throw new SubscriptionRequiredException();

        var movie = await _movieRepository.GetByIdAsync(request.MovieId)
            ?? throw new MovieNotFoundException();

        var entry = WatchHistoryEntry.Builder.Create(userId, movie.Id);

        await _watchHistoryRepository.AddAsync(entry);

        return ToResponse(entry, movie);
    }

    public async Task<PagedResponseDto<WatchHistoryEntryResponseDto>> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var (items, totalCount) = await _watchHistoryRepository.GetByUserIdAsync(userId, page, pageSize);

        return new PagedResponseDto<WatchHistoryEntryResponseDto>
        {
            Items = items.Select(entry => ToResponse(entry, entry.Movie)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)),
        };
    }

    private static WatchHistoryEntryResponseDto ToResponse(WatchHistoryEntry entry, Movie? movie) => new()
    {
        Id = entry.Id,
        MovieId = entry.MovieId,
        MovieTitle = movie?.Title ?? string.Empty,
        ReleaseYear = movie?.ReleaseDate?.Year,
        PosterUrl = movie?.PosterUrl,
        WatchedAt = entry.WatchedAt,
    };
}
