using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Lists;
using MoviesArchive.Domain.Models.Lists.Dtos;

namespace MoviesArchive.Infra.Services;

public class MovieListService : IMovieListService
{
    private readonly IMovieListRepository _movieListRepository;

    public MovieListService(
        IMovieListRepository movieListRepository)
    {
        _movieListRepository = movieListRepository;
    }

    public async Task<ListResponseDto> CreateAsync(Guid userId, CreateListRequestDto request)
    {
        var title = request.Title.Trim();

        if (await _movieListRepository.TitleExistsAsync(userId, title))
            throw new ListTitleAlreadyInUseException();

        var newList = MovieList.Builder.Create(userId, title, NormalizeDescription(request.Description), request.IsPublic);

        await _movieListRepository.AddAsync(newList);

        return ToResponse(newList);
    }

    public async Task<ListResponseDto> GetByIdAsync(Guid listId, Guid requesterId)
    {
        var list = await _movieListRepository.GetVisibleByIdAsync(listId, requesterId);

        if (list == null)
            throw new ListNotFoundException();

        return ToResponse(list);
    }

    public async Task<List<ListResponseDto>> GetByUserIdAsync(Guid ownerId, Guid requesterId)
    {
        var lists = await _movieListRepository.GetVisibleByUserIdAsync(ownerId, requesterId);
        return lists.Select(ToResponse).ToList();
    }

    public async Task<ListResponseDto> UpdateAsync(Guid listId, Guid userId, UpdateListRequestDto request)
    {
        var list = await GetOwnedListAsync(listId, userId);
        var title = request.Title.Trim();

        if (await _movieListRepository.TitleExistsAsync(userId, title, listId))
            throw new ListTitleAlreadyInUseException();

        list.Title = title;
        list.Description = NormalizeDescription(request.Description);
        list.IsPublic = request.IsPublic;

        await _movieListRepository.UpdateAsync(list);

        return ToResponse(list);
    }

    public async Task DeleteAsync(Guid listId, Guid userId)
    {
        var list = await GetOwnedListAsync(listId, userId);
        await _movieListRepository.DeleteAsync(list);
    }

    public async Task<ListResponseDto> AddItemAsync(Guid listId, Guid userId, AddListItemRequestDto request)
    {
        var list = await GetOwnedListAsync(listId, userId);

        if (list.Items.Any(item => item.MovieId == request.MovieId))
            throw new MovieAlreadyInListException();

        if (!await _movieListRepository.MovieExistsAsync(request.MovieId))
            throw new MovieNotFoundException();

        var newItem = new ListMovie(list.Id, request.MovieId, list.Items.Count);

        await _movieListRepository.AddItemAsync(newItem);

        return await GetByIdAsync(listId, userId);
    }

    public async Task<ListResponseDto> RemoveItemAsync(Guid listId, Guid userId, Guid movieId)
    {
        var list = await GetOwnedListAsync(listId, userId);

        var item = list.Items.FirstOrDefault(item => item.MovieId == movieId);
        if (item == null)
            throw new ListItemNotFoundException();

        list.Items.Remove(item);

        // Reorganiza as posições restantes para não deixar buracos na sequência
        var position = 0;
        foreach (var remainingItem in list.Items.OrderBy(i => i.Position))
            remainingItem.Position = position++;

        await _movieListRepository.RemoveItemAsync(item);

        return ToResponse(list);
    }

    public async Task<ListResponseDto> UpdateItemsOrderAsync(Guid listId, Guid userId, UpdateListItemsOrderRequestDto request)
    {
        var list = await GetOwnedListAsync(listId, userId);

        var currentMovieIds = list.Items.Select(item => item.MovieId).ToHashSet();
        var hasDuplicates = request.MovieIds.Distinct().Count() != request.MovieIds.Count;

        if (hasDuplicates || !currentMovieIds.SetEquals(request.MovieIds))
            throw new InvalidListOrderException();

        for (var position = 0; position < request.MovieIds.Count; position++)
            list.Items.First(item => item.MovieId == request.MovieIds[position]).Position = position;

        await _movieListRepository.UpdateAsync(list);

        return ToResponse(list);
    }

    private async Task<MovieList> GetOwnedListAsync(Guid listId, Guid userId)
    {
        var list = await _movieListRepository.GetByIdAsync(listId);

        // Lista privada de outro usuário é tratada como inexistente
        if (list == null || (list.IsPrivate && !list.IsOwnedBy(userId)))
            throw new ListNotFoundException();

        if (!list.IsOwnedBy(userId))
            throw new ListAccessDeniedException();

        return list;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static ListResponseDto ToResponse(MovieList list) => new()
    {
        Id = list.Id,
        UserId = list.UserId,
        Title = list.Title,
        Description = list.Description,
        IsPublic = list.IsPublic,
        CreatedAt = list.CreatedAt,
        Items = list.Items
            .OrderBy(item => item.Position)
            .Select(item => new ListItemResponseDto
            {
                MovieId = item.MovieId,
                MovieTitle = item.Movie?.Title ?? string.Empty,
                Position = item.Position,
                AddedAt = item.AddedAt
            })
            .ToList()
    };
}
