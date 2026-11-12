using Fundoo.Notes.Application.DTOs.Notes;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;

        public NoteService(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        public async Task<NoteResponse> CreateAsync(Guid userId,CreateNoteRequest request,CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ArgumentException("Note title is required.");
            }

            var note = new Note
            {
                Id = Guid.NewGuid(), // guid created 
                UserId = userId,
                Title = request.Title.Trim(),
                Content = request.Content,
                Color = string.IsNullOrWhiteSpace(request.Color) ? "#FFFFFF" : request.Color.Trim(),
                IsPinned = request.IsPinned,
                IsArchived = false,
                IsTrashed = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var createdNote = await _noteRepository.CreateAsync(note, cancellationToken);

            return MapToResponse(createdNote);
        }

        public async Task<IReadOnlyList<NoteResponse>> GetActiveNotesAsync(Guid userId,CancellationToken cancellationToken)
        {
            var notes = await _noteRepository.GetActiveNotesAsync(
                userId,
                cancellationToken);

            return notes.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<NoteResponse>> GetArchivedNotesAsync(Guid userId,CancellationToken cancellationToken)
        {
            var notes = await _noteRepository.GetArchivedNotesAsync(
                userId,
                cancellationToken);

            return notes.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<NoteResponse>> GetTrashedNotesAsync(Guid userId,CancellationToken cancellationToken)
        {
            var notes = await _noteRepository.GetTrashedNotesAsync(
                userId,
                cancellationToken);

            return notes.Select(MapToResponse).ToList();
        }

        public async Task<NoteResponse?> GetByIdAsync(Guid id,Guid userId,CancellationToken cancellationToken)
        {
            var note = await _noteRepository.GetByIdAsync(
                id,
                userId,
                cancellationToken);

            return note is null ? null : MapToResponse(note);
        }

        public async Task<NoteResponse?> UpdateAsync(
            Guid id,
            Guid userId,
            UpdateNoteRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ArgumentException("Note title is required.");
            }

            var note = await _noteRepository.GetByIdAsync(
                id,
                userId,
                cancellationToken);

            if (note is null || note.IsTrashed)
            {
                return null;
            }

            note.Title = request.Title.Trim();
            note.Content = request.Content;
            note.Color = string.IsNullOrWhiteSpace(request.Color)
                ? "#FFFFFF"
                : request.Color.Trim();
            note.IsPinned = request.IsPinned;
            note.UpdatedAtUtc = DateTime.UtcNow;

            var updated = await _noteRepository.UpdateAsync(
                note,
                cancellationToken);

            if (!updated)
            {
                return null;
            }

            return MapToResponse(note);
        }

        public Task<bool> DeleteAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _noteRepository.DeleteAsync(
                id,
                userId,
                cancellationToken);
        }

        public Task<bool> ArchiveAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _noteRepository.SetArchivedAsync(
                id,
                userId,
                true,
                cancellationToken);
        }

        public Task<bool> UnarchiveAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _noteRepository.SetArchivedAsync(
                id,
                userId,
                false,
                cancellationToken);
        }

        public Task<bool> TrashAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _noteRepository.SetTrashedAsync(
                id,
                userId,
                true,
                cancellationToken);
        }

        public Task<bool> RestoreAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _noteRepository.SetTrashedAsync(
                id,
                userId,
                false,
                cancellationToken);
        }

        public Task<bool> PinAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _noteRepository.SetPinnedAsync(
                id,
                userId,
                true,
                cancellationToken);
        }

        public Task<bool> UnpinAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _noteRepository.SetPinnedAsync(
                id,
                userId,
                false,
                cancellationToken);
        }

        // ------------------Helper Methods ---------------------------------------
        private static NoteResponse MapToResponse(Note note)
        {
            return new NoteResponse
            {
                Id = note.Id,
                UserId = note.UserId,
                Title = note.Title,
                Content = note.Content,
                Color = note.Color,
                IsPinned = note.IsPinned,
                IsArchived = note.IsArchived,
                IsTrashed = note.IsTrashed,
                CreatedAtUtc = note.CreatedAtUtc,
                UpdatedAtUtc = note.UpdatedAtUtc,
                LabelIds = note.NoteLabels.Select(noteLabel => noteLabel.LabelId).ToList()
            };
        }
    }
}