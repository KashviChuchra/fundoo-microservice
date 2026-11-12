using Fundoo.Notes.Application.DTOs.Labels;

namespace Fundoo.Notes.Application.Interfaces
{
    public interface ILabelService
    {
        Task<LabelResponse> CreateAsync(
            Guid userId,
            CreateLabelRequest request,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<LabelResponse>> GetAllAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<LabelResponse?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> RenameAsync(
            Guid id,
            Guid userId,
            RenameLabelRequest request,
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
