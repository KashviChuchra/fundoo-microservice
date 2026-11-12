using System.Security.Claims;
using Asp.Versioning;
using Fundoo.Notes.Application.DTOs.Notes;
using Fundoo.Notes.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notes.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/notes")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;

        public NotesController(INoteService noteService)
        {
            _noteService = noteService;
        }

        private Guid? GetUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (Guid.TryParse(userIdValue, out var userId))
            {
                return userId;
            }

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNoteRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null) return Unauthorized();

            try
            {
                var note = await _noteService.CreateAsync(userId.Value,request, cancellationToken);

                return CreatedAtAction(nameof(GetById),new { id = note.Id },note);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null) return Unauthorized();

            var notes = await _noteService.GetActiveNotesAsync(userId.Value, cancellationToken);

            return Ok(notes);
        }

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchived(CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null) return Unauthorized();

            var notes = await _noteService.GetArchivedNotesAsync(userId.Value,cancellationToken);

            return Ok(notes);
        }

        [HttpGet("trash")]
        public async Task<IActionResult> GetTrashed(
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var notes = await _noteService.GetTrashedNotesAsync(
                userId.Value,
                cancellationToken);

            return Ok(notes);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var note = await _noteService.GetByIdAsync(
                id,
                userId.Value,
                cancellationToken);

            if (note is null)
            {
                return NotFound(new { message = "Note not found." });
            }

            return Ok(note);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateNoteRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            try
            {
                var note = await _noteService.UpdateAsync(
                    id,
                    userId.Value,
                    request,
                    cancellationToken);

                if (note is null)
                {
                    return NotFound(new { message = "Note not found." });
                }

                return Ok(note);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var deleted = await _noteService.DeleteAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = "Note not found." });
            }

            return NoContent();
        }

        [HttpPatch("{id:guid}/archive")]
        public async Task<IActionResult> Archive(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var updated = await _noteService.ArchiveAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { message = "Note not found." });
            }

            return NoContent();
        }

        [HttpPatch("{id:guid}/unarchive")]
        public async Task<IActionResult> Unarchive(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var updated = await _noteService.UnarchiveAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { message = "Note not found." });
            }

            return NoContent();
        }

        [HttpPatch("{id:guid}/trash")]
        public async Task<IActionResult> Trash(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var updated = await _noteService.TrashAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { message = "Note not found." });
            }

            return NoContent();
        }

        [HttpPatch("{id:guid}/restore")]
        public async Task<IActionResult> Restore(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var updated = await _noteService.RestoreAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { message = "Note not found." });
            }

            return NoContent();
        }

        [HttpPatch("{id:guid}/pin")]
        public async Task<IActionResult> Pin(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var updated = await _noteService.PinAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { message = "Note not found." });
            }

            return NoContent();
        }

        [HttpPatch("{id:guid}/unpin")]
        public async Task<IActionResult> Unpin(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var updated = await _noteService.UnpinAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { message = "Note not found." });
            }

            return NoContent();
        }
    }
}