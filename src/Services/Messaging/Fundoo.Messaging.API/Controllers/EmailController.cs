using Fundoo.Messaging.Application.DTOs;
using Fundoo.Messaging.Application.Interfaces;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Messaging.API.Controllers;

/// <summary>
/// Handles email-sending requests for the Fundoo Messaging microservice.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    // timeout on route
    [RequestTimeout("NotificationTimeout")]
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Send([FromBody] SendEmailRequest request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to send email to {Recipient}.", request.To);

        var success = await _emailService.SendAsync(request, cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Email to {Recipient} could not be delivered.", request.To);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Failed to send the email. Please try again later." });
        }

        return Ok(new { message = $"Email successfully sent to {request.To}." });
    }
}
