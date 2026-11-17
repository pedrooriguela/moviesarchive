using MoviesArchive.Domain.Models.Admin;
using MoviesArchive.Domain.Models.Admin.Dtos;
using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Domain.Interfaces;

// Cadastro, edição, remoção e restauração de filmes pelo administrador
public interface IMovieAdminService
{
    Task<PagedResponseDto<AdminMovieDto>> SearchAsync(string? query, AdminMovieStatus status, int page, int pageSize);
    Task<AdminMovieDto> GetAsync(Guid movieId);
    Task<AdminMovieDto> CreateAsync(SaveMovieRequestDto request);
    Task<AdminMovieDto> UpdateAsync(Guid movieId, SaveMovieRequestDto request);
    Task<AdminMovieDto> RemoveAsync(Guid movieId);
    Task<AdminMovieDto> RestoreAsync(Guid movieId);
}
