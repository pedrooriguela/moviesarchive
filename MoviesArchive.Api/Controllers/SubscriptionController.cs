using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Subscriptions.Dtos;

namespace MoviesArchive.Api.Controllers;

// Planos, assinatura do usuário logado e convites do plano familiar.
// Toda ação devolve a situação atualizada (SubscriptionStateDto), para o front só redesenhar.
// Erros de regra: 404, 403 ou 409 com { code, message }.
[ApiController]
[Authorize]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(
        ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    // Planos à venda; aberto também para a vitrine de visitantes
    [AllowAnonymous]
    [HttpGet("api/plans")]
    public async Task<IActionResult> GetPlansAsync() =>
        Ok(await _subscriptionService.GetPlansAsync());

    [HttpGet("api/subscriptions/me")]
    public Task<IActionResult> GetMyStateAsync() =>
        RunAsync(userId => _subscriptionService.GetStateAsync(userId));

    // Contratar (só maiores de 18, sem assinatura)
    [HttpPost("api/subscriptions")]
    public Task<IActionResult> SubscribeAsync([FromBody] SubscribeRequestDto request) =>
        RunAsync(userId => _subscriptionService.SubscribeAsync(userId, request.PlanId!.Value));

    // Upgrade ou downgrade (só o titular)
    [HttpPut("api/subscriptions/me/plan")]
    public Task<IActionResult> ChangePlanAsync([FromBody] SubscribeRequestDto request) =>
        RunAsync(userId => _subscriptionService.ChangePlanAsync(userId, request.PlanId!.Value));

    // Cancelar (só o titular): todos os integrantes voltam a ser contas gratuitas
    [HttpDelete("api/subscriptions/me")]
    public Task<IActionResult> CancelAsync() =>
        RunAsync(userId => _subscriptionService.CancelAsync(userId));

    // Sair do plano familiar de outra pessoa
    [HttpDelete("api/subscriptions/me/membership")]
    public Task<IActionResult> LeaveAsync() =>
        RunAsync(userId => _subscriptionService.LeaveAsync(userId));

    [HttpPost("api/subscriptions/me/invites")]
    public Task<IActionResult> InviteAsync([FromBody] FamilyInviteRequestDto request) =>
        RunAsync(userId => _subscriptionService.InviteAsync(userId, request.UserName));

    [HttpDelete("api/subscriptions/me/invites/{inviteId:guid}")]
    public Task<IActionResult> CancelInviteAsync([FromRoute] Guid inviteId) =>
        RunAsync(userId => _subscriptionService.CancelInviteAsync(userId, inviteId));

    [HttpDelete("api/subscriptions/me/members/{memberId:guid}")]
    public Task<IActionResult> RemoveMemberAsync([FromRoute] Guid memberId) =>
        RunAsync(userId => _subscriptionService.RemoveMemberAsync(userId, memberId));

    [HttpPost("api/invites/{inviteId:guid}/accept")]
    public Task<IActionResult> AcceptInviteAsync([FromRoute] Guid inviteId) =>
        RunAsync(userId => _subscriptionService.AcceptInviteAsync(userId, inviteId));

    [HttpPost("api/invites/{inviteId:guid}/decline")]
    public Task<IActionResult> DeclineInviteAsync([FromRoute] Guid inviteId) =>
        RunAsync(userId => _subscriptionService.DeclineInviteAsync(userId, inviteId));

    private async Task<IActionResult> RunAsync(Func<Guid, Task<SubscriptionStateDto>> action)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            return Ok(await action(userId.Value));
        }
        catch (SubscriptionException ex)
        {
            var body = new { code = ex.Code, message = ex.Message };
            return ex.Kind switch
            {
                SubscriptionErrorKind.NotFound => NotFound(body),
                SubscriptionErrorKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
                _ => Conflict(body),
            };
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
