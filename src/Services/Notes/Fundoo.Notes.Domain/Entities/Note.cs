namespace Fundoo.Notes.Domain.Entities
{
    public class Note
    {
        public Guid Id { get; set; } // gloabl unique identifier , Created in C# code using Guid.NewGuid()
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
        public bool IsTrashed { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public List<NoteLabel> NoteLabels { get; set; } = new();  // between notes and labels - many to many rs, for noramlization
    }
}