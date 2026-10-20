using MoviesArchive.Domain.Dtos;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users.Dtos;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Infra.Services;

namespace MoviesArchive.Api.Controllers;
[ApiController]
public class EmailController : Controller
{
    private readonly IEmailService _emailService;
    private readonly PasswordResetService _passwordResetService;
    public EmailController(
        IEmailService emailService,
        PasswordResetService passwordResetService)
    {
        _emailService = emailService;
        _passwordResetService = passwordResetService;
    }

    [HttpPost("api/email/send")]
    public async Task<IActionResult> SendEmailAsync([FromBody] SendEmailDto request)
    {
        try
        {
            await _emailService.SendAsync(request.ToEmail, request.Subject, request.Body);
        }
        catch(Exception ex)
        {
            return BadRequest(ex.Message);
        }
        return Ok();
    }
}