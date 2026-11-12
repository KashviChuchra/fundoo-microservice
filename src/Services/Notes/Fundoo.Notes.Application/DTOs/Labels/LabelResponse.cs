namespace Fundoo.Notes.Application.DTOs.Labels
{
    public class LabelResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}