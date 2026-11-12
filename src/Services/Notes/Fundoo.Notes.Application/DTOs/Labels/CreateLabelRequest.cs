using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Application.DTOs.Labels
{
    public class CreateLabelRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }
}