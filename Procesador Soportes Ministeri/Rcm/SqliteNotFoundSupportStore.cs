using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public interface INotFoundSupportStore
    {
        Task AddAsync(NotFoundSupportRecord record);
        Task<List<NotFoundSupportRecord>> GetByStatusAsync(string status);
        Task MarkCompletedAsync(int id);
    }

    public class SqliteNotFoundSupportStore : INotFoundSupportStore
    {
        private readonly string _connectionString;

        public SqliteNotFoundSupportStore(string dbPath)
        {
            if (dbPath == null) throw new ArgumentNullException(nameof(dbPath));
            var directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            _connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA journal_mode=WAL";
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS NotFoundSupport (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SupportPath TEXT NOT NULL,
                            Invoice TEXT NOT NULL,
                            Status TEXT NOT NULL,
                            CreatedAt DATETIME NOT NULL
                        )
                    ";
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE INDEX IF NOT EXISTS idx_notfound_status
                        ON NotFoundSupport(Status)
                    ";
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task AddAsync(NotFoundSupportRecord record)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO NotFoundSupport (SupportPath, Invoice, Status, CreatedAt)
                        VALUES (@supportPath, @invoice, @status, @createdAt)
                    ";
                    command.Parameters.AddWithValue("@supportPath", record.SupportPath);
                    command.Parameters.AddWithValue("@invoice", record.Invoice);
                    command.Parameters.AddWithValue("@status", record.Status);
                    command.Parameters.AddWithValue("@createdAt", record.CreatedAt);

                    await Task.Run(() => command.ExecuteNonQuery());
                }
            }
        }

        public async Task<List<NotFoundSupportRecord>> GetByStatusAsync(string status)
        {
            var result = new List<NotFoundSupportRecord>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, SupportPath, Invoice, Status, CreatedAt FROM NotFoundSupport WHERE Status = @status ORDER BY CreatedAt";
                    command.Parameters.AddWithValue("@status", status);

                    using (var reader = await Task.Run(() => command.ExecuteReader() as SQLiteDataReader))
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new NotFoundSupportRecord
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                SupportPath = reader["SupportPath"].ToString(),
                                Invoice = reader["Invoice"].ToString(),
                                Status = reader["Status"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }
            }
            return result;
        }

        public async Task MarkCompletedAsync(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE NotFoundSupport SET Status = @status WHERE Id = @id";
                    command.Parameters.AddWithValue("@status", NotFoundSupportStatus.Completed);
                    command.Parameters.AddWithValue("@id", id);

                    await Task.Run(() => command.ExecuteNonQuery());
                }
            }
        }
    }
}
