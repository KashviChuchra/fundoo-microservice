
using System.Security.Claims;
using Asp.Versioning;
using Fundoo.Notes.Application.DTOs.Notes;
using Fundoo.Notes.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notes.Api.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/notes")]
    [Authorize]
    public class NotesControllerV2 : ControllerBase
    {
        private readonly INoteService _noteService;

        public NotesControllerV2(INoteService noteService)
        {
            _noteService = noteService;
        }

        private Guid? GetUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");

            if (Guid.TryParse(userIdValue, out var userId))
            {
                return userId;
            }

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateNoteRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            try
            {
                var note = await _noteService.CreateAsync(
                    userId.Value,
                    request,
                    cancellationToken);

                return CreatedAtAction(
                    nameof(Create),
                    new { version = "2.0" },
                    new
                    {
                        ApiVersion = "V2",
                        Message = "Note created successfully using API V2",
                        Data = note
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
