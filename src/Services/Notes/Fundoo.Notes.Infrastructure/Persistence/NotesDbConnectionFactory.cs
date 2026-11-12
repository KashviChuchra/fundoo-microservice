using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Persistence
{
    public class NotesDbConnectionFactory
    {
        private readonly string _connectionString;

        public NotesDbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("NotesConnection")
                ?? throw new InvalidOperationException("NotesConnection connection string is not configured.");
        }

        public async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new NpgsqlConnection(_connectionString);

            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }
}