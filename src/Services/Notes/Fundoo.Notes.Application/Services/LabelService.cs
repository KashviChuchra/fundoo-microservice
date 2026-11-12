using Fundoo.Notes.Application.DTOs.Labels;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services
{
    public class LabelService : ILabelService
    {
        private readonly ILabelRepository _labelRepository;

        public LabelService(ILabelRepository labelRepository)
        {
            _labelRepository = labelRepository;
        }

        public async Task<LabelResponse> CreateAsync(
            Guid userId,
            CreateLabelRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Label name is required.");
            }

            var label = new Label
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            var createdLabel = await _labelRepository.CreateAsync(
                label,
                cancellationToken);

            return MapToResponse(createdLabel);
        }

        public async Task<IReadOnlyList<LabelResponse>> GetAllAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var labels = await _labelRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

            return labels.Select(MapToResponse).ToList();
        }

        public async Task<LabelResponse?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var label = await _labelRepository.GetByIdAsync(
                id,
                userId,
                cancellationToken);

            return label is null ? null : MapToResponse(label);
        }

        public async Task<bool> RenameAsync(
            Guid id,
            Guid userId,
            RenameLabelRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Label name is required.");
            }

            return await _labelRepository.RenameAsync(
                id,
                userId,
                request.Name.Trim(),
                cancellationToken);
        }

        public Task<bool> DeleteAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _labelRepository.DeleteAsync(
                id,
                userId,
                cancellationToken);
        }

        public Task<bool> AddToNoteAsync(
            Guid noteId,
            Guid labelId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _labelRepository.AddToNoteAsync(
                noteId,
                labelId,
                userId,
                cancellationToken);
        }

        public Task<bool> RemoveFromNoteAsync(
            Guid noteId,
            Guid labelId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _labelRepository.RemoveFromNoteAsync(
                noteId,
                labelId,
                userId,
                cancellationToken);
        }

        private static LabelResponse MapToResponse(Label label)
        {
            return new LabelResponse
            {
                Id = label.Id,
                UserId = label.UserId,
                Name = label.Name,
                CreatedAtUtc = label.CreatedAtUtc
            };
        }
    }
}