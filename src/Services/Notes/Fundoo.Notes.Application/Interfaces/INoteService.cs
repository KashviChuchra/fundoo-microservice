using Fundoo.Notes.Application.DTOs.Notes;

namespace Fundoo.Notes.Application.Interfaces
{
    public interface INoteService
    {
        Task<NoteResponse> CreateAsync(Guid userId,CreateNoteRequest request,CancellationToken cancellationToken);
        Task<IReadOnlyList<NoteResponse>> GetActiveNotesAsync(Guid userId,CancellationToken cancellationToken);
        Task<IReadOnlyList<NoteResponse>> GetArchivedNotesAsync(Guid userId,CancellationToken cancellationToken);
        Task<IReadOnlyList<NoteResponse>> GetTrashedNotesAsync(Guid userId,CancellationToken cancellationToken);
        Task<NoteResponse?> GetByIdAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<NoteResponse?> UpdateAsync(Guid id,Guid userId,UpdateNoteRequest request,CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> ArchiveAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> UnarchiveAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> TrashAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> RestoreAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> PinAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> UnpinAsync(Guid id,Guid userId,CancellationToken cancellationToken);
    }
}