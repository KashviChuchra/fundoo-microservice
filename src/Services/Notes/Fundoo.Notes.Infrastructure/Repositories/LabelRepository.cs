using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Fundoo.Notes.Infrastructure.Persistence;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Repositories
{
    public class LabelRepository : ILabelRepository
    {
        private readonly NotesDbConnectionFactory _connectionFactory;

        public LabelRepository(NotesDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Label> CreateAsync(Label label,CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                INSERT INTO labels
                (
                    id,
                    user_id,
                    name,
                    created_at_utc
                )
                VALUES
                (
                    @id,
                    @userId,
                    @name,
                    @createdAtUtc
                );
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", label.Id);
            command.Parameters.AddWithValue("userId", label.UserId);
            command.Parameters.AddWithValue("name", label.Name);
            command.Parameters.AddWithValue("createdAtUtc",label.CreatedAtUtc);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return label;
        }
        public async Task<IReadOnlyList<Label>> GetByUserIdAsync(Guid userId,CancellationToken cancellationToken)
        {
            var labels = new List<Label>();

            await using var connection =await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                SELECT id, user_id, name, created_at_utc
                FROM labels
                WHERE user_id = @userId
                ORDER BY created_at_utc DESC;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("userId", userId);

            await using var reader =await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                labels.Add(MapLabel(reader));
            }

            return labels;
        }
        public async Task<Label?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                SELECT id, user_id, name, created_at_utc
                FROM labels
                WHERE id = @id
                  AND user_id = @userId;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", userId);

            await using var reader =await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapLabel(reader);
        }
        public async Task<bool> RenameAsync(Guid id,Guid userId,string name,CancellationToken cancellationToken)
        {
            await using var connection =await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                UPDATE labels
                SET name = @name
                WHERE id = @id
                  AND user_id = @userId;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("name", name);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }
        public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                DELETE FROM labels
                WHERE id = @id
                  AND user_id = @userId;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", userId);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            return rowsAffected > 0;
        }
        public async Task<bool> AddToNoteAsync(Guid noteId, Guid labelId, Guid userId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                INSERT INTO note_labels (note_id, label_id)
                SELECT n.id, l.id
                FROM notes n
                INNER JOIN labels l
                    ON l.id = @labelId
                   AND l.user_id = @userId
                WHERE n.id = @noteId
                  AND n.user_id = @userId
                ON CONFLICT (note_id, label_id) DO NOTHING;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("noteId", noteId);
            command.Parameters.AddWithValue("labelId", labelId);
            command.Parameters.AddWithValue("userId", userId);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            return rowsAffected > 0;
        }
        public async Task<bool> RemoveFromNoteAsync(Guid noteId, Guid labelId,Guid userId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = """
                DELETE FROM note_labels nl
                USING notes n, labels l
                WHERE nl.note_id = n.id
                  AND nl.label_id = l.id
                  AND n.id = @noteId
                  AND l.id = @labelId
                  AND n.user_id = @userId
                  AND l.user_id = @userId;
                """;

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("noteId", noteId);
            command.Parameters.AddWithValue("labelId", labelId);
            command.Parameters.AddWithValue("userId", userId);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            return rowsAffected > 0;
        }


        // ---------------- Helper Methods ----------------------

        private static Label MapLabel(NpgsqlDataReader reader)
        {
            return new Label
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("created_at_utc"))
            };
        }
    }
}