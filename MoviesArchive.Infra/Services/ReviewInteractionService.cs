using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Domain.Models.Reviews.Dtos;

namespace MoviesArchive.Infra.Services;

public class ReviewInteractionService : IReviewInteractionService
{
    private readonly IReviewInteractionRepository _interactionRepository;
    private readonly IMovieRepository _movieRepository;

    public ReviewInteractionService(
        IReviewInteractionRepository interactionRepository,
        IMovieRepository movieRepository)
    {
        _interactionRepository = interactionRepository;
        _movieRepository = movieRepository;
    }

    public async Task<List<ReplyResponseDto>> GetRepliesAsync(Guid reviewId, Guid? viewerId)
    {
        await GetVisibleReviewAsync(reviewId);

        var replies = await _interactionRepository.GetRepliesAsync(reviewId, viewerId);
        return replies.Select(ToResponse).ToList();
    }

    public async Task<ReplyResponseDto> AddReplyAsync(Guid reviewId, Guid userId, CreateReplyRequestDto request)
    {
        await GetVisibleReviewAsync(reviewId);

        // Resposta a uma resposta: ela tem de ser desta avaliação e ainda existir
        if (request.ParentReplyId is Guid parentId)
        {
            var parent = await _interactionRepository.GetReplyAsync(parentId);
            if (parent == null || parent.ReviewId != reviewId)
                throw new ReplyNotFoundException();
            if (parent.IsDeleted)
                throw new InvalidReplyException("Essa resposta foi excluída e não pode mais ser respondida.");
        }

        var reply = ReviewReply.Builder.Create(reviewId, userId, request.ParentReplyId, request.Comment);
        await _interactionRepository.AddReplyAsync(reply);

        var created = await _interactionRepository.GetReplyWithAuthorAsync(reply.Id, userId)
            ?? throw new ReplyNotFoundException();
        return ToResponse(created);
    }

    // Quem escreveu exclui. Se a resposta já tiver respostas, ela vira "resposta excluída"
    // para a conversa não perder o fio; senão, sai de vez.
    public async Task DeleteReplyAsync(Guid replyId, Guid userId)
    {
        var reply = await _interactionRepository.GetReplyAsync(replyId);
        if (reply == null || reply.IsDeleted)
            throw new ReplyNotFoundException();
        if (reply.UserId != userId)
            throw new ReplyForbiddenException();

        if (await _interactionRepository.HasChildRepliesAsync(reply.Id))
        {
            reply.MarkAsDeleted();
            await _interactionRepository.UpdateReplyAsync(reply);
            return;
        }

        var parentId = reply.ParentReplyId;
        await _interactionRepository.DeleteReplyAsync(reply);

        // Sobe a conversa apagando respostas já excluídas que ficaram sem nenhuma resposta
        while (parentId is Guid id)
        {
            var parent = await _interactionRepository.GetReplyAsync(id);
            if (parent == null || !parent.IsDeleted || await _interactionRepository.HasChildRepliesAsync(parent.Id))
                break;

            parentId = parent.ParentReplyId;
            await _interactionRepository.DeleteReplyAsync(parent);
        }
    }

    public async Task<LikeStateDto> SetReviewLikeAsync(Guid reviewId, Guid userId, bool liked)
    {
        await GetVisibleReviewAsync(reviewId);

        await _interactionRepository.SetReviewLikeAsync(userId, reviewId, liked);
        return new LikeStateDto
        {
            LikeCount = await _interactionRepository.CountReviewLikesAsync(reviewId),
            LikedByMe = liked,
        };
    }

    public async Task<LikeStateDto> SetReplyLikeAsync(Guid replyId, Guid userId, bool liked)
    {
        var reply = await _interactionRepository.GetReplyAsync(replyId);
        if (reply == null || reply.IsDeleted)
            throw new ReplyNotFoundException();
        await GetVisibleReviewAsync(reply.ReviewId);

        await _interactionRepository.SetReplyLikeAsync(userId, replyId, liked);
        return new LikeStateDto
        {
            LikeCount = await _interactionRepository.CountReplyLikesAsync(replyId),
            LikedByMe = liked,
        };
    }

    // A avaliação precisa existir e o filme precisa estar visível para quem pede
    // (filmes acima da classificação indicativa do usuário se comportam como inexistentes)
    private async Task<Review> GetVisibleReviewAsync(Guid reviewId)
    {
        var review = await _interactionRepository.GetReviewAsync(reviewId)
            ?? throw new ReviewNotFoundByIdException();

        if (await _movieRepository.GetByIdAsync(review.MovieId) == null)
            throw new ReviewNotFoundByIdException();

        return review;
    }

    private static ReplyResponseDto ToResponse(ReplyWithAuthor item) => new()
    {
        Id = item.Reply.Id,
        ReviewId = item.Reply.ReviewId,
        ParentReplyId = item.Reply.ParentReplyId,
        UserId = item.Reply.UserId,
        UserName = item.AuthorName,
        // Mesma rota usada pelo UserController
        UserPhotoUrl = item.AuthorHasPhoto ? $"/api/users/{item.Reply.UserId}/photo" : null,
        Comment = item.Reply.IsDeleted ? string.Empty : item.Reply.Comment,
        IsDeleted = item.Reply.IsDeleted,
        CreatedAt = item.Reply.CreatedAt,
        LikeCount = item.LikeCount,
        LikedByMe = item.LikedByViewer,
    };
}
