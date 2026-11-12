namespace Fundoo.Notes.Application.DTOs.Notes
{
    public class NoteResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
        public bool IsTrashed { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public List<Guid> LabelIds { get; set; } = new();  // note attached to labels
    }
}