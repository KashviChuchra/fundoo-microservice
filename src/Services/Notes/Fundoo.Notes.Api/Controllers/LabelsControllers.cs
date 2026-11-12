
using System.Security.Claims;
using Fundoo.Notes.Application.DTOs.Labels;
using Fundoo.Notes.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notes.Api.Controllers
{
    [ApiController]
    [Route("api/labels")]
    [Authorize]
    public class LabelsController : ControllerBase
    {
        private readonly ILabelService _labelService;

        public LabelsController(ILabelService labelService)
        {
            _labelService = labelService;
        }

        private Guid? GetUserId()
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (Guid.TryParse(userIdValue, out var userId))
            {
                return userId;
            }

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLabelRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            try
            {
                var label = await _labelService.CreateAsync(
                    userId.Value,
                    request,
                    cancellationToken);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = label.Id },
                    label);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var labels = await _labelService.GetAllAsync(
                userId.Value,
                cancellationToken);

            return Ok(labels);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var label = await _labelService.GetByIdAsync(
                id,
                userId.Value,
                cancellationToken);

            if (label is null)
            {
                return NotFound(new { message = "Label not found." });
            }

            return Ok(label);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Rename(
            Guid id,
            [FromBody] RenameLabelRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            try
            {
                var renamed = await _labelService.RenameAsync(
                    id,
                    userId.Value,
                    request,
                    cancellationToken);

                if (!renamed)
                {
                    return NotFound(new { message = "Label not found." });
                }

                return NoContent();
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

            var deleted = await _labelService.DeleteAsync(
                id,
                userId.Value,
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = "Label not found." });
            }

            return NoContent();
        }

        [HttpPost("{labelId:guid}/notes/{noteId:guid}")]
        public async Task<IActionResult> AddToNote(
            Guid labelId,
            Guid noteId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var added = await _labelService.AddToNoteAsync(
                noteId,
                labelId,
                userId.Value,
                cancellationToken);

            if (!added)
            {
                return NotFound(new
                {
                    message = "Note or label not found, or the label is already attached."
                });
            }

            return NoContent();
        }

        [HttpDelete("{labelId:guid}/notes/{noteId:guid}")]
        public async Task<IActionResult> RemoveFromNote(
            Guid labelId,
            Guid noteId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId is null)
                return Unauthorized();

            var removed = await _labelService.RemoveFromNoteAsync(
                noteId,
                labelId,
                userId.Value,
                cancellationToken);

            if (!removed)
            {
                return NotFound(new
                {
                    message = "Note-label association not found."
                });
            }

            return NoContent();
        }
    }
}