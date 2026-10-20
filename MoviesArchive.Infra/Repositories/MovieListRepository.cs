using MoviesArchive.Infra.Database;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Lists;
using Microsoft.EntityFrameworkCore;

namespace MoviesArchive.Infra.Repositories;

public class MovieListRepository : IMovieListRepository
{
    private readonly MoviesArchiveDbContext _dbContext;
    public MovieListRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MovieList list)
    {
        await _dbContext.AddAsync(list);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(MovieList list)
    {
        if (_dbContext.Entry(list).State == EntityState.Detached)
            _dbContext.Update(list);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(MovieList list)
    {
        _dbContext.Remove(list);
        await _dbContext.SaveChangesAsync();
    }

    // Sem filtro de visibilidade: usar apenas em operações do dono da lista
    public async Task<MovieList?> GetByIdAsync(Guid id)
    {
        return await ListsWithItems()
            .FirstOrDefaultAsync(list => list.Id == id);
    }

    public async Task<MovieList?> GetVisibleByIdAsync(Guid id, Guid requesterId)
    {
        return await VisibleTo(requesterId)
            .FirstOrDefaultAsync(list => list.Id == id);
    }

    public async Task<List<MovieList>> GetVisibleByUserIdAsync(Guid ownerId, Guid requesterId)
    {
        return await VisibleTo(requesterId)
            .Where(list => list.UserId == ownerId)
            .OrderByDescending(list => list.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> TitleExistsAsync(Guid userId, string title, Guid? ignoredListId = null)
    {
        return await _dbContext.Lists.AnyAsync(list =>
            list.UserId == userId &&
            list.Title == title &&
            (ignoredListId == null || list.Id != ignoredListId));
    }

    public async Task<bool> MovieExistsAsync(Guid movieId)
    {
        return await _dbContext.Movies.AnyAsync(movie => movie.Id == movieId);
    }

    public async Task AddItemAsync(ListMovie item)
    {
        await _dbContext.ListMovies.AddAsync(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(ListMovie item)
    {
        _dbContext.ListMovies.Remove(item);
        await _dbContext.SaveChangesAsync();
    }

    private IQueryable<MovieList> ListsWithItems() =>
        _dbContext.Lists
            .Include(list => list.Items.OrderBy(item => item.Position))
            .ThenInclude(item => item.Movie);

    // Consultas de terceiros ignoram listas privadas; o dono continua vendo todas as suas
    private IQueryable<MovieList> VisibleTo(Guid requesterId) =>
        ListsWithItems()
            .Where(list => list.UserId == requesterId || list.IsPublic);
}
