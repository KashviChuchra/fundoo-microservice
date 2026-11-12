using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Application.DTOs.Notes
{
    public class UpdateNoteRequest
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        [StringLength(30)]
        public string Color { get; set; } = "#FFFFFF";

        public bool IsPinned { get; set; }
    }
}