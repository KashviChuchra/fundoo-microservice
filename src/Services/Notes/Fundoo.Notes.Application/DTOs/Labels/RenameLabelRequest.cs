using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Application.DTOs.Labels
{
    public class RenameLabelRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }
}