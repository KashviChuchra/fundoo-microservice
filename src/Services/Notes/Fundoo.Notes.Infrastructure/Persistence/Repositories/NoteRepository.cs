using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Fundoo.Notes.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace Fundoo.Notes.Infrastructure.Persistence.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly NotesDbConnectionFactory _connectionFactory;
        public NoteRepository(NotesDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Note> CreateAsync(Note note,CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                INSERT INTO notes
                (
                    id, user_id, title, content, color,
                    is_pinned, is_archived, is_trashed,
                    created_at_utc, updated_at_utc
                )
                VALUES
                (
                    @id, @userId, @title, @content, @color,
                    @isPinned, @isArchived, @isTrashed,
                    @createdAtUtc, @updatedAtUtc
                );
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", note.Id);
            command.Parameters.AddWithValue("userId", note.UserId);
            command.Parameters.AddWithValue("title", note.Title);

            command.Parameters.Add("content", NpgsqlDbType.Text).Value =
                (object?)note.Content ?? DBNull.Value;

            command.Parameters.AddWithValue("color", note.Color);
            command.Parameters.AddWithValue("isPinned", note.IsPinned);
            command.Parameters.AddWithValue("isArchived", note.IsArchived);
            command.Parameters.AddWithValue("isTrashed", note.IsTrashed);
            command.Parameters.AddWithValue("createdAtUtc", note.CreatedAtUtc);
            command.Parameters.AddWithValue("updatedAtUtc", note.UpdatedAtUtc);

            await command.ExecuteNonQueryAsync(cancellationToken); // modify db, no result set

            return note;
        }

        public async Task<IReadOnlyList<Note>> GetActiveNotesAsync(Guid userId, CancellationToken cancellationToken)
        {
            const string query = """
                SELECT *
                FROM notes
                WHERE (user_id = @userId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @userId))
                  AND is_archived = FALSE
                  AND is_trashed = FALSE
                ORDER BY updated_at_utc DESC;
                """;

            return await GetNotesAsync(query, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<Note>> GetArchivedNotesAsync(Guid userId, CancellationToken cancellationToken)
        {
            const string query = """
                SELECT *
                FROM notes
                WHERE (user_id = @userId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @userId))
                  AND is_archived = TRUE
                  AND is_trashed = FALSE
                ORDER BY updated_at_utc DESC;
                """;

            return await GetNotesAsync(query, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<Note>> GetTrashedNotesAsync(Guid userId,CancellationToken cancellationToken)
        {
            const string query = """
                SELECT *
                FROM notes
                WHERE (user_id = @userId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @userId))
                  AND is_trashed = TRUE
                ORDER BY updated_at_utc DESC;
                """;

            return await GetNotesAsync(query, userId, cancellationToken);
        }

        public async Task<Note?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                SELECT *
                FROM notes
                WHERE id = @id
                  AND (user_id = @userId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @userId));
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var note = MapNote(reader);

            await reader.CloseAsync();

            note.NoteLabels = await GetNoteLabelsAsync(connection, note.Id, cancellationToken);

            return note;
        }

        public async Task<DateTime?> UpdateAsync(Note note, Guid actorId, DateTime expectedUpdatedAtUtc, CancellationToken cancellationToken)
        {
            
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                UPDATE notes
                SET title = @title,
                    content = @content,
                    color = @color,
                    is_pinned = @isPinned,
                    updated_at_utc = GREATEST(@updatedAtUtc, updated_at_utc + INTERVAL '1 microsecond')
                WHERE id = @id
                  AND (user_id = @actorId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @actorId))
                  AND is_trashed = FALSE
                  AND updated_at_utc = @expectedUpdatedAtUtc
                RETURNING updated_at_utc;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", note.Id);
            command.Parameters.AddWithValue("actorId", actorId);
            command.Parameters.AddWithValue("title", note.Title);
            command.Parameters.Add("content", NpgsqlDbType.Text).Value =(object?)note.Content ?? DBNull.Value;
            command.Parameters.AddWithValue("color", note.Color);
            command.Parameters.AddWithValue("isPinned", note.IsPinned);

            // Use the timestamp assigned by NoteService.
            command.Parameters.AddWithValue("updatedAtUtc", note.UpdatedAtUtc);
            command.Parameters.AddWithValue("expectedUpdatedAtUtc", expectedUpdatedAtUtc);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is DateTime updatedAtUtc ? updatedAtUtc : null;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                DELETE FROM notes
                WHERE id = @id
                  AND user_id = @userId;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", userId);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            return rowsAffected > 0;
        }

        public async Task<bool> SetArchivedAsync(Guid id, Guid userId, bool isArchived, CancellationToken cancellationToken)
        {
            const string query = """
                UPDATE notes
                SET is_archived = @isArchived,
                    updated_at_utc = GREATEST(@updatedAtUtc, updated_at_utc + INTERVAL '1 microsecond')
                WHERE id = @id
                  AND (user_id = @userId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @userId))
                  AND is_trashed = FALSE;
                """;

            return await ExecuteStateUpdateAsync(query, id, userId, "isArchived", isArchived, cancellationToken);
        }

        public async Task<bool> SetTrashedAsync(Guid id, Guid userId, bool isTrashed, CancellationToken cancellationToken)
        {
            const string query = """
                UPDATE notes
                SET is_trashed = @isTrashed,
                    is_archived = CASE
                        WHEN @isTrashed THEN FALSE
                        ELSE is_archived
                    END,
                    updated_at_utc = GREATEST(@updatedAtUtc, updated_at_utc + INTERVAL '1 microsecond')
                WHERE id = @id
                  AND (user_id = @userId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @userId));
                """;

            return await ExecuteStateUpdateAsync(query,id,userId,"isTrashed",isTrashed,cancellationToken);
        }

        public async Task<bool> SetPinnedAsync(Guid id,Guid userId,bool isPinned,CancellationToken cancellationToken)
        {
            const string query = """
                UPDATE notes
                SET is_pinned = @isPinned,
                    updated_at_utc = GREATEST(@updatedAtUtc, updated_at_utc + INTERVAL '1 microsecond')
                WHERE id = @id
                  AND (user_id = @userId OR EXISTS (SELECT 1 FROM note_collaborators c WHERE c.note_id = notes.id AND c.user_id = @userId))
                  AND is_trashed = FALSE;
                """;

            return await ExecuteStateUpdateAsync(query,id,userId,"isPinned",isPinned,cancellationToken);
        }

        public async Task<bool> AddCollaboratorAsync(Guid noteId, Guid ownerId, Guid collaboratorId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = """
                INSERT INTO note_collaborators (note_id, user_id)
                SELECT id, @collaboratorId FROM notes
                WHERE id = @noteId AND user_id = @ownerId AND is_trashed = FALSE
                ON CONFLICT (note_id, user_id) DO NOTHING;
                """;
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("noteId", noteId);
            command.Parameters.AddWithValue("ownerId", ownerId);
            command.Parameters.AddWithValue("collaboratorId", collaboratorId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        public async Task<bool> RemoveCollaboratorAsync(Guid noteId, Guid ownerId, Guid collaboratorId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = """
                DELETE FROM note_collaborators c
                USING notes n
                WHERE c.note_id = n.id AND n.id = @noteId AND n.user_id = @ownerId
                  AND c.user_id = @collaboratorId;
                """;
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("noteId", noteId);
            command.Parameters.AddWithValue("ownerId", ownerId);
            command.Parameters.AddWithValue("collaboratorId", collaboratorId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        public async Task<IReadOnlyList<Guid>?> GetCollaboratorsAsync(Guid noteId, Guid ownerId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = """
                SELECT c.user_id FROM note_collaborators c
                INNER JOIN notes n ON n.id = c.note_id
                WHERE n.id = @noteId AND n.user_id = @ownerId
                ORDER BY c.created_at_utc;
                """;
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("noteId", noteId);
            command.Parameters.AddWithValue("ownerId", ownerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var collaborators = new List<Guid>();
            while (await reader.ReadAsync(cancellationToken)) collaborators.Add(reader.GetGuid(0));
            if (collaborators.Count > 0) return collaborators;

            await reader.CloseAsync();
            await using var existsCommand = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM notes WHERE id = @noteId AND user_id = @ownerId);", connection);
            existsCommand.Parameters.AddWithValue("noteId", noteId);
            existsCommand.Parameters.AddWithValue("ownerId", ownerId);
            return (bool)(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? false) ? collaborators : null;
        }


        // ---------------- Helper Methods ----------------------

        private async Task<IReadOnlyList<Note>> GetNotesAsync(string query, Guid userId, CancellationToken cancellationToken)
        {
            var notes = new List<Note>();

            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("userId", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                notes.Add(MapNote(reader));
            }

            await reader.CloseAsync();

            // Load label associations for every returned note.
            foreach (var note in notes)
            {
                note.NoteLabels = await GetNoteLabelsAsync(connection, note.Id, cancellationToken);
            }
            return notes;
        }
        private static async Task<List<NoteLabel>> GetNoteLabelsAsync(NpgsqlConnection connection,Guid noteId, CancellationToken cancellationToken)
        {
            var noteLabels = new List<NoteLabel>();

            const string query = """
                SELECT note_id, label_id
                FROM note_labels
                WHERE note_id = @noteId;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("noteId", noteId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                noteLabels.Add(new NoteLabel
                {
                    NoteId = reader.GetGuid(reader.GetOrdinal("note_id")),

                    LabelId = reader.GetGuid(reader.GetOrdinal("label_id"))
                });
            }

            return noteLabels;
        }
        private async Task<bool> ExecuteStateUpdateAsync(string query, Guid id, Guid userId, string parameterName,bool value, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue(parameterName, value);
            var now = DateTime.UtcNow;
            command.Parameters.AddWithValue("updatedAtUtc", new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc));

            int rowsAffected =await command.ExecuteNonQueryAsync(cancellationToken);

            return rowsAffected > 0;
        }
        private static Note MapNote(NpgsqlDataReader reader)
        {
            return new Note
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Content = reader.IsDBNull(reader.GetOrdinal("content")) ? null: reader.GetString(reader.GetOrdinal("content")),
                Color = reader.GetString(reader.GetOrdinal("color")),
                IsPinned = reader.GetBoolean(reader.GetOrdinal("is_pinned")),
                IsArchived = reader.GetBoolean(reader.GetOrdinal("is_archived")),
                IsTrashed = reader.GetBoolean(reader.GetOrdinal("is_trashed")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc")),
                UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("updated_at_utc"))
            };
        }
    }
}
