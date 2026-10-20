using Microsoft.AspNetCore.Authorization;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Lists.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MoviesArchive.Api.Controllers;

[ApiController]
[Authorize]
public class ListController : Controller
{
    private readonly IMovieListService _movieListService;

    public ListController(
        IMovieListService movieListService)
    {
        _movieListService = movieListService;
    }

    [HttpPost("api/lists")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateListRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var list = await _movieListService.CreateAsync(userId.Value, request);
            return Ok(list);
        }
        catch (ListTitleAlreadyInUseException ex)
        {
            return Conflict(new { field = "title", message = ex.Message });
        }
    }

    [HttpGet("api/lists/me")]
    public async Task<IActionResult> GetMyListsAsync()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        var lists = await _movieListService.GetByUserIdAsync(userId.Value, userId.Value);
        return Ok(lists);
    }

    [HttpGet("api/users/{ownerId:guid}/lists")]
    public async Task<IActionResult> GetUserListsAsync([FromRoute] Guid ownerId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        var lists = await _movieListService.GetByUserIdAsync(ownerId, userId.Value);
        return Ok(lists);
    }

    [HttpGet("api/lists/{listId:guid}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid listId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var list = await _movieListService.GetByIdAsync(listId, userId.Value);
            return Ok(list);
        }
        catch (ListNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("api/lists/{listId:guid}")]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid listId, [FromBody] UpdateListRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var list = await _movieListService.UpdateAsync(listId, userId.Value, request);
            return Ok(list);
        }
        catch (ListNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ListAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ListTitleAlreadyInUseException ex)
        {
            return Conflict(new { field = "title", message = ex.Message });
        }
    }

    [HttpDelete("api/lists/{listId:guid}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid listId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            await _movieListService.DeleteAsync(listId, userId.Value);
            return NoContent();
        }
        catch (ListNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ListAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("api/lists/{listId:guid}/items")]
    public async Task<IActionResult> AddItemAsync([FromRoute] Guid listId, [FromBody] AddListItemRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var list = await _movieListService.AddItemAsync(listId, userId.Value, request);
            return Ok(list);
        }
        catch (ListNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { field = "movieId", message = ex.Message });
        }
        catch (ListAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (MovieAlreadyInListException ex)
        {
            return Conflict(new { field = "movieId", message = ex.Message });
        }
    }

    [HttpDelete("api/lists/{listId:guid}/items/{movieId:guid}")]
    public async Task<IActionResult> RemoveItemAsync([FromRoute] Guid listId, [FromRoute] Guid movieId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var list = await _movieListService.RemoveItemAsync(listId, userId.Value, movieId);
            return Ok(list);
        }
        catch (ListNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ListItemNotFoundException ex)
        {
            return NotFound(new { field = "movieId", message = ex.Message });
        }
        catch (ListAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPut("api/lists/{listId:guid}/items/order")]
    public async Task<IActionResult> UpdateItemsOrderAsync([FromRoute] Guid listId, [FromBody] UpdateListItemsOrderRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var list = await _movieListService.UpdateItemsOrderAsync(listId, userId.Value, request);
            return Ok(list);
        }
        catch (ListNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ListAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidListOrderException ex)
        {
            return BadRequest(new { field = "movieIds", message = ex.Message });
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
