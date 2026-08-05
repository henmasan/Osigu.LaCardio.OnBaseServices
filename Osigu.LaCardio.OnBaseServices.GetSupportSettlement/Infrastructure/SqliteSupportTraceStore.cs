using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure
{
    public class SqliteSupportTraceStore : ISupportTraceStore
    {
        private readonly string _databasePath;
        private readonly ILogger<SqliteSupportTraceStore> _logger;

        public SqliteSupportTraceStore(AppsettingConfiguration config, ILogger<SqliteSupportTraceStore> logger)
        {
            _databasePath = config.Configuration.RcmStagingPath + "/support_trace.db";
            _logger = logger;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS SupportTrace (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                InvoiceNumber TEXT NOT NULL,
                                SupportType TEXT NOT NULL,
                                AttemptCount INTEGER DEFAULT 0,
                                LastAttemptTime DATETIME,
                                Status TEXT,
                                ErrorMessage TEXT,
                                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                                FilePath TEXT
                            );

                            CREATE INDEX IF NOT EXISTS idx_invoice_support
                            ON SupportTrace(InvoiceNumber, SupportType);
                        ";
                        command.ExecuteNonQuery();
                    }
                }
                _logger.LogInformation($"SQLite database initialized at {_databasePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing SQLite database");
            }
        }

        public async Task<SupportTraceRecord> AddTraceAsync(SupportTraceRecord record)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO SupportTrace (InvoiceNumber, SupportType, AttemptCount, Status, FilePath)
                            VALUES (@invoice, @supportType, @attempts, @status, @filePath);
                            SELECT last_insert_rowid();
                        ";
                        command.Parameters.AddWithValue("@invoice", record.InvoiceNumber ?? "");
                        command.Parameters.AddWithValue("@supportType", record.SupportType ?? "");
                        command.Parameters.AddWithValue("@attempts", record.AttemptCount);
                        command.Parameters.AddWithValue("@status", record.Status ?? "Pending");
                        command.Parameters.AddWithValue("@filePath", record.FilePath ?? "");

                        var id = (long?)await command.ExecuteScalarAsync() ?? 0;
                        record.Id = (int)id;
                        record.CreatedAt = DateTime.UtcNow;
                        record.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding trace record");
            }
            return record;
        }

        public async Task<SupportTraceRecord> GetTraceAsync(int id)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM SupportTrace WHERE Id = @id";
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = await command.ExecuteReaderAsync())
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
                _logger.LogError(ex, "Error getting trace record");
            }
            return null;
        }

        public async Task<SupportTraceRecord> GetPendingTraceAsync(string invoiceNumber, string supportType)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM SupportTrace WHERE InvoiceNumber = @invoice AND SupportType = @supportType AND Status = 'Pending'";
                        command.Parameters.AddWithValue("@invoice", invoiceNumber);
                        command.Parameters.AddWithValue("@supportType", supportType);

                        using (var reader = await command.ExecuteReaderAsync())
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
                _logger.LogError(ex, "Error getting pending trace");
            }
            return null;
        }

        public async Task<List<SupportTraceRecord>> GetPendingTracesAsync()
        {
            var traces = new List<SupportTraceRecord>();
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM SupportTrace WHERE Status = 'Pending' ORDER BY CreatedAt ASC";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                traces.Add(MapReaderToRecord(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending traces");
            }
            return traces;
        }

        public async Task UpdateTraceAsync(SupportTraceRecord record)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            UPDATE SupportTrace
                            SET AttemptCount = @attempts, Status = @status, ErrorMessage = @error,
                                LastAttemptTime = @lastAttempt, UpdatedAt = CURRENT_TIMESTAMP
                            WHERE Id = @id
                        ";
                        command.Parameters.AddWithValue("@attempts", record.AttemptCount);
                        command.Parameters.AddWithValue("@status", record.Status ?? "Pending");
                        command.Parameters.AddWithValue("@error", record.ErrorMessage ?? "");
                        command.Parameters.AddWithValue("@lastAttempt", record.LastAttemptTime == DateTime.MinValue ? DBNull.Value : record.LastAttemptTime);
                        command.Parameters.AddWithValue("@id", record.Id);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trace record");
            }
        }

        public async Task<bool> ExistsAsync(string invoiceNumber, string supportType)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM SupportTrace WHERE InvoiceNumber = @invoice AND SupportType = @supportType";
                        command.Parameters.AddWithValue("@invoice", invoiceNumber);
                        command.Parameters.AddWithValue("@supportType", supportType);

                        var count = (long?)await command.ExecuteScalarAsync() ?? 0;
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking trace existence");
                return false;
            }
        }

        private SupportTraceRecord MapReaderToRecord(SqliteDataReader reader)
        {
            return new SupportTraceRecord
            {
                Id = reader.GetInt32(0),
                InvoiceNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
                SupportType = reader.IsDBNull(2) ? null : reader.GetString(2),
                AttemptCount = reader.GetInt32(3),
                LastAttemptTime = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                Status = reader.IsDBNull(5) ? null : reader.GetString(5),
                ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.IsDBNull(7) ? DateTime.UtcNow : reader.GetDateTime(7),
                UpdatedAt = reader.IsDBNull(8) ? DateTime.UtcNow : reader.GetDateTime(8),
                FilePath = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
        }
    }
}
