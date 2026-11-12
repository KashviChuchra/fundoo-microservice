using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces
{
    public interface INoteRepository
    {
        // cancellation token - when user cancel the request/ logout
        // The server detects the user disconnected, triggers the token, and stops the operation immediately.

        Task<Note> CreateAsync(Note note, CancellationToken cancellationToken); 
        Task<IReadOnlyList<Note>> GetActiveNotesAsync(Guid userId,CancellationToken cancellationToken);
        Task<IReadOnlyList<Note>> GetArchivedNotesAsync(Guid userId,CancellationToken cancellationToken);
        Task<IReadOnlyList<Note>> GetTrashedNotesAsync(Guid userId,CancellationToken cancellationToken);
        Task<Note?> GetByIdAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> UpdateAsync(Note note,CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid id,Guid userId,CancellationToken cancellationToken);
        Task<bool> SetArchivedAsync(Guid id,Guid userId,bool isArchived,CancellationToken cancellationToken);
        Task<bool> SetTrashedAsync(Guid id,Guid userId,bool isTrashed,CancellationToken cancellationToken);
        Task<bool> SetPinnedAsync(Guid id,Guid userId,bool isPinned,CancellationToken cancellationToken);
    }
}