using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces
{
    public interface ILabelRepository
    {
        Task<Label> CreateAsync(
            Label label,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<Label>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<Label?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> RenameAsync(
            Guid id,
            Guid userId,
            string name,
            CancellationToken cancellationToken);

        Task<bool> DeleteAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> AddToNoteAsync(
            Guid noteId,
            Guid labelId,
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> RemoveFromNoteAsync(
            Guid noteId,
            Guid labelId,
            Guid userId,
            CancellationToken cancellationToken);
    }
}