using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

namespace ScheduleCentral.Services
{
    public static class DemoDbContextConnectionCache
    {
        private static readonly ConcurrentDictionary<string, SqliteConnection> Connections = new(StringComparer.OrdinalIgnoreCase);

        public static SqliteConnection GetConnection(string sessionId)
        {
            return Connections.GetOrAdd(sessionId, id =>
            {
                // Create a shared in-memory connection
                var conn = new SqliteConnection($"Data Source=demo_{id};Mode=Memory;Cache=Shared");
                conn.Open();
                return conn;
            });
        }

        public static void ClearConnection(string sessionId)
        {
            if (Connections.TryRemove(sessionId, out var conn))
            {
                conn.Close();
                conn.Dispose();
            }
        }
    }
}
