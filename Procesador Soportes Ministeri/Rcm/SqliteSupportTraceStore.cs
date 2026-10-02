using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using System.IO;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public interface ISupportTraceStore
    {
        Task AddTraceAsync(SupportTraceRecord record);
        Task<SupportTraceRecord> GetTraceAsync(int id);
        Task<List<SupportTraceRecord>> GetPendingTracesAsync();
        Task UpdateTraceAsync(SupportTraceRecord record);
        Task<bool> ExistsAsync(int id);
    }

    public class SqliteSupportTraceStore : ISupportTraceStore
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public SqliteSupportTraceStore(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            var directory = Path.GetDirectoryName(_dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            _connectionString = $"Data Source={_dbPath};Version=3;";
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

                var createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS SupportTrace (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InvoiceNumber TEXT NOT NULL,
                        SupportType TEXT NOT NULL,
                        AttemptCount INTEGER DEFAULT 0,
                        LastAttemptTime DATETIME,
                        Status TEXT,
                        ErrorMessage TEXT,
                        CreatedAt DATETIME,
                        UpdatedAt DATETIME,
                        FilePath TEXT,
                        ProcessId TEXT,
                        UniqueVerificationCode TEXT,
                        SourceCode TEXT,
                        Observation TEXT
                    )
                ";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createTableQuery;
                    command.ExecuteNonQuery();
                }

                var indexQuery = @"
                    CREATE INDEX IF NOT EXISTS idx_invoice_support
                    ON SupportTrace(InvoiceNumber, SupportType)
                ";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = indexQuery;
                    command.ExecuteNonQuery();
                }

                MigrateSchema(connection);
            }
        }

        private void MigrateSchema(SQLiteConnection connection)
        {
            var columnsToCheck = new[] { "ProcessId", "UniqueVerificationCode", "SourceCode", "Observation" };

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(SupportTrace)";
                var existingColumns = new HashSet<string>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingColumns.Add(reader["name"].ToString().ToLower());
                    }
                }

                foreach (var col in columnsToCheck)
                {
                    if (!existingColumns.Contains(col.ToLower()))
                    {
                        string dataType = "TEXT";
                        using (var addColumnCmd = connection.CreateCommand())
                        {
                            addColumnCmd.CommandText = $"ALTER TABLE SupportTrace ADD COLUMN {col} {dataType}";
                            addColumnCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public async Task AddTraceAsync(SupportTraceRecord record)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 5;
                        command.CommandText = @"
                            INSERT INTO SupportTrace
                            (InvoiceNumber, SupportType, AttemptCount, Status, CreatedAt, UpdatedAt, FilePath, ProcessId, UniqueVerificationCode, SourceCode)
                            VALUES (@invoiceNumber, @supportType, @attemptCount, @status, @createdAt, @updatedAt, @filePath, @processId, @uniqueVerificationCode, @sourceCode)
                        ";

                        command.Parameters.AddWithValue("@invoiceNumber", record.InvoiceNumber ?? "");
                        command.Parameters.AddWithValue("@supportType", record.SupportType ?? "");
                        command.Parameters.AddWithValue("@attemptCount", record.AttemptCount);
                        command.Parameters.AddWithValue("@status", record.Status ?? "Pending");
                        command.Parameters.AddWithValue("@createdAt", record.CreatedAt);
                        command.Parameters.AddWithValue("@updatedAt", record.UpdatedAt ?? DateTime.UtcNow);
                        command.Parameters.AddWithValue("@filePath", record.FilePath ?? "");
                        command.Parameters.AddWithValue("@processId", record.ProcessId ?? "");
                        command.Parameters.AddWithValue("@uniqueVerificationCode", record.UniqueVerificationCode ?? "");
                        command.Parameters.AddWithValue("@sourceCode", record.EadCode ?? "");

                        await Task.Run(() => command.ExecuteNonQuery());
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<SupportTraceRecord> GetTraceAsync(int id)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 5;
                        command.CommandText = "SELECT * FROM SupportTrace WHERE Id = @id";
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = await Task.Run(() => command.ExecuteReader() as SQLiteDataReader))
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapReaderToRecord(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return null;
        }

        public async Task<List<SupportTraceRecord>> GetPendingTracesAsync()
        {
            var result = new List<SupportTraceRecord>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 5;
                        command.CommandText = "SELECT * FROM SupportTrace WHERE Status = 'Pending' ORDER BY CreatedAt";

                        using (var reader = await Task.Run(() => command.ExecuteReader() as SQLiteDataReader))
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(MapReaderToRecord(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;
        }

        public async Task UpdateTraceAsync(SupportTraceRecord record)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 5;
                        command.CommandText = @"
                            UPDATE SupportTrace
                            SET AttemptCount = @attemptCount,
                                Status = @status,
                                ErrorMessage = @errorMessage,
                                LastAttemptTime = @lastAttemptTime,
                                Observation = @observation,
                                UpdatedAt = @updatedAt
                            WHERE Id = @id
                        ";

                        command.Parameters.AddWithValue("@id", record.Id);
                        command.Parameters.AddWithValue("@attemptCount", record.AttemptCount);
                        command.Parameters.AddWithValue("@status", record.Status ?? "");
                        command.Parameters.AddWithValue("@errorMessage", record.ErrorMessage ?? "");
                        command.Parameters.AddWithValue("@lastAttemptTime", record.LastAttemptTime ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@observation", record.Observation ?? "");
                        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

                        await Task.Run(() => command.ExecuteNonQuery());
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 5;
                        command.CommandText = "SELECT COUNT(*) FROM SupportTrace WHERE Id = @id";
                        command.Parameters.AddWithValue("@id", id);

                        var result = await Task.Run(() => (long)command.ExecuteScalar());
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private SupportTraceRecord MapReaderToRecord(SQLiteDataReader reader)
        {
            return new SupportTraceRecord
            {
                Id = Convert.ToInt32(reader[0]),
                InvoiceNumber = reader[1]?.ToString() ?? "",
                SupportType = reader[2]?.ToString() ?? "",
                AttemptCount = Convert.ToInt32(reader[3]),
                LastAttemptTime = reader[4] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[4]),
                Status = reader[5]?.ToString() ?? "",
                ErrorMessage = reader[6]?.ToString(),
                CreatedAt = Convert.ToDateTime(reader[7]),
                UpdatedAt = reader[8] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[8]),
                FilePath = reader[9]?.ToString(),
                ProcessId = reader[10]?.ToString(),
                UniqueVerificationCode = reader[11]?.ToString(),
                EadCode = reader[12]?.ToString(),
                Observation = reader[13]?.ToString()
            };
        }
    }
}
