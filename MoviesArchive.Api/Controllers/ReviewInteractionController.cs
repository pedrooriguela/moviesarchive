using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Reviews.Dtos;

namespace MoviesArchive.Api.Controllers;

// Respostas às avaliações (que também podem ser respondidas) e curtidas
[ApiController]
[Authorize]
public class ReviewInteractionController : Controller
{
    private readonly IReviewInteractionService _interactionService;

    public ReviewInteractionController(
        IReviewInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    // Conversa inteira da avaliação, da resposta mais antiga à mais nova.
    // A árvore é montada no front pelo ParentReplyId.
    [HttpGet("api/reviews/{reviewId:guid}/replies")]
    public async Task<IActionResult> GetRepliesAsync([FromRoute] Guid reviewId)
    {
        try
        {
            return Ok(await _interactionService.GetRepliesAsync(reviewId, GetAuthenticatedUserId()));
        }
        catch (ReviewNotFoundByIdException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("api/reviews/{reviewId:guid}/replies")]
    public async Task<IActionResult> AddReplyAsync([FromRoute] Guid reviewId, [FromBody] CreateReplyRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var reply = await _interactionService.AddReplyAsync(reviewId, userId.Value, request);
            return StatusCode(StatusCodes.Status201Created, reply);
        }
        catch (ReviewNotFoundByIdException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ReplyNotFoundException ex)
        {
            return NotFound(new { field = "parentReplyId", message = ex.Message });
        }
        catch (InvalidReplyException ex)
        {
            return BadRequest(new { field = "comment", message = ex.Message });
        }
    }

    [HttpDelete("api/replies/{replyId:guid}")]
    public async Task<IActionResult> DeleteReplyAsync([FromRoute] Guid replyId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            await _interactionService.DeleteReplyAsync(replyId, userId.Value);
            return NoContent();
        }
        catch (ReplyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ReplyForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    // Curtir (PUT) e descurtir (DELETE) uma avaliação; os dois devolvem { likeCount, likedByMe }
    [HttpPut("api/reviews/{reviewId:guid}/like")]
    public Task<IActionResult> LikeReviewAsync([FromRoute] Guid reviewId) => SetReviewLikeAsync(reviewId, true);

    [HttpDelete("api/reviews/{reviewId:guid}/like")]
    public Task<IActionResult> UnlikeReviewAsync([FromRoute] Guid reviewId) => SetReviewLikeAsync(reviewId, false);

    [HttpPut("api/replies/{replyId:guid}/like")]
    public Task<IActionResult> LikeReplyAsync([FromRoute] Guid replyId) => SetReplyLikeAsync(replyId, true);

    [HttpDelete("api/replies/{replyId:guid}/like")]
    public Task<IActionResult> UnlikeReplyAsync([FromRoute] Guid replyId) => SetReplyLikeAsync(replyId, false);

    private async Task<IActionResult> SetReviewLikeAsync(Guid reviewId, bool liked)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            return Ok(await _interactionService.SetReviewLikeAsync(reviewId, userId.Value, liked));
        }
        catch (ReviewNotFoundByIdException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private async Task<IActionResult> SetReplyLikeAsync(Guid replyId, bool liked)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            return Ok(await _interactionService.SetReplyLikeAsync(replyId, userId.Value, liked));
        }
        catch (Exception ex) when (ex is ReplyNotFoundException or ReviewNotFoundByIdException)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
