using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public ReviewRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Review?> GetByUserAndMovieAsync(Guid userId, Guid movieId) =>
        _dbContext.Reviews.FirstOrDefaultAsync(review => review.UserId == userId && review.MovieId == movieId);

    public async Task<(List<ReviewWithAuthor> Items, int TotalCount)> GetByMovieAsync(Guid movieId, int page, int pageSize)
    {
        var reviews = _dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.MovieId == movieId);

        var totalCount = await reviews.CountAsync();

        // Mais recentes primeiro; o Id desempata para a paginação ficar estável
        var rows = await reviews
            .Join(
                _dbContext.Users.AsNoTracking(),
                review => review.UserId,
                user => user.Id,
                (review, user) => new { Review = review, user.Name, HasPhoto = user.PhotoData != null })
            .OrderByDescending(row => row.Review.CreatedAt)
            .ThenBy(row => row.Review.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rows
            .Select(row => new ReviewWithAuthor(row.Review, row.Name, row.HasPhoto))
            .ToList();

        return (items, totalCount);
    }

    public async Task<Dictionary<decimal, int>> CountByRatingAsync(Guid movieId)
    {
        var counts = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.MovieId == movieId)
            .GroupBy(review => review.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() })
            .ToListAsync();

        return counts.ToDictionary(item => item.Rating, item => item.Count);
    }

    public async Task AddAsync(Review review)
    {
        await _dbContext.Reviews.AddAsync(review);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Review review)
    {
        if (_dbContext.Entry(review).State == EntityState.Detached)
            _dbContext.Reviews.Update(review);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Review review)
    {
        _dbContext.Reviews.Remove(review);
        await _dbContext.SaveChangesAsync();
    }
}
