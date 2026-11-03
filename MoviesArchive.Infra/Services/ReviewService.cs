using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Domain.Models.Reviews.Dtos;

namespace MoviesArchive.Infra.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IUserRepository _userRepository;

    public ReviewService(
        IReviewRepository reviewRepository,
        IWatchlistRepository watchlistRepository,
        IMovieRepository movieRepository,
        IUserRepository userRepository)
    {
        _reviewRepository = reviewRepository;
        _watchlistRepository = watchlistRepository;
        _movieRepository = movieRepository;
        _userRepository = userRepository;
    }

    public async Task<MovieReviewsResponseDto> GetMovieReviewsAsync(Guid movieId, int page, int pageSize)
    {
        await EnsureMovieExistsAsync(movieId);

        var (items, totalCount) = await _reviewRepository.GetByMovieAsync(movieId, page, pageSize);
        var counts = await _reviewRepository.CountByRatingAsync(movieId);

        // Índice 0 = 0,5 estrela ... índice 9 = 5 estrelas
        var histogram = new int[10];
        foreach (var (rating, count) in counts)
        {
            var index = (int)(rating * 2) - 1;
            if (index is >= 0 and < 10)
                histogram[index] += count;
        }

        var ratedCount = counts.Values.Sum();
        decimal? average = ratedCount == 0
            ? null
            : Math.Round(counts.Sum(pair => pair.Key * pair.Value) / ratedCount, 2);

        return new MovieReviewsResponseDto
        {
            Items = items.Select(item => ToResponse(item.Review, item.AuthorName, item.AuthorHasPhoto)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)),
            AverageRating = average,
            Histogram = histogram,
        };
    }

    public async Task<MovieUserStateDto> GetUserStateAsync(Guid movieId, Guid userId)
    {
        await EnsureMovieExistsAsync(movieId);

        var review = await _reviewRepository.GetByUserAndMovieAsync(userId, movieId);

        return new MovieUserStateDto
        {
            InWatchlist = await _watchlistRepository.ContainsAsync(userId, movieId),
            Review = review == null ? null : await ToResponseAsync(review),
        };
    }

    // Um usuário tem no máximo uma avaliação por filme: se já existir, é substituída
    public async Task<ReviewResponseDto> UpsertAsync(Guid movieId, Guid userId, UpsertReviewRequestDto request)
    {
        await EnsureMovieExistsAsync(movieId);

        var review = await _reviewRepository.GetByUserAndMovieAsync(userId, movieId);
        if (review == null)
        {
            review = new Review(userId, movieId, request.Rating, request.Comment);
            await _reviewRepository.AddAsync(review);
        }
        else
        {
            review.Update(request.Rating, request.Comment);
            await _reviewRepository.UpdateAsync(review);
        }

        return await ToResponseAsync(review);
    }

    public async Task DeleteAsync(Guid movieId, Guid userId)
    {
        var review = await _reviewRepository.GetByUserAndMovieAsync(userId, movieId)
            ?? throw new ReviewNotFoundException();

        await _reviewRepository.DeleteAsync(review);
    }

    private async Task EnsureMovieExistsAsync(Guid movieId)
    {
        if (await _movieRepository.GetByIdAsync(movieId) == null)
            throw new MovieNotFoundException();
    }

    private async Task<ReviewResponseDto> ToResponseAsync(Review review)
    {
        var user = await _userRepository.GetByIdAsync(review.UserId);
        return ToResponse(review, user?.Name ?? string.Empty, user?.PhotoData != null);
    }

    private static ReviewResponseDto ToResponse(Review review, string userName, bool hasPhoto) => new()
    {
        Id = review.Id,
        MovieId = review.MovieId,
        UserId = review.UserId,
        UserName = userName,
        // Mesma rota usada pelo UserController
        UserPhotoUrl = hasPhoto ? $"/api/users/{review.UserId}/photo" : null,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt,
    };
}
