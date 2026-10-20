using MoviesArchive.Domain.Models.Lists.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IMovieListService
{
    Task<ListResponseDto> CreateAsync(Guid userId, CreateListRequestDto request);
    Task<ListResponseDto> GetByIdAsync(Guid listId, Guid requesterId);
    Task<List<ListResponseDto>> GetByUserIdAsync(Guid ownerId, Guid requesterId);
    Task<ListResponseDto> UpdateAsync(Guid listId, Guid userId, UpdateListRequestDto request);
    Task DeleteAsync(Guid listId, Guid userId);
    Task<ListResponseDto> AddItemAsync(Guid listId, Guid userId, AddListItemRequestDto request);
    Task<ListResponseDto> RemoveItemAsync(Guid listId, Guid userId, Guid movieId);
    Task<ListResponseDto> UpdateItemsOrderAsync(Guid listId, Guid userId, UpdateListItemsOrderRequestDto request);
}
