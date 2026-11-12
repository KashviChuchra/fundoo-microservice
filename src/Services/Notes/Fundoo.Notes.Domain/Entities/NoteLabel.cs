namespace Fundoo.Notes.Domain.Entities
{
    public class NoteLabel
    {
        public Guid NoteId { get; set; }
        public Guid LabelId { get; set; }
        public Note? Note { get; set; }
        public Label? Label { get; set; }
    }
}