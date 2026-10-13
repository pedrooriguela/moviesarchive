using MoviesArchive.Domain.Dtos;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace MoviesArchive.Api.Controllers;
[ApiController]
public class EmailController : Controller
{
    private readonly IEmailService _emailService;
    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
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