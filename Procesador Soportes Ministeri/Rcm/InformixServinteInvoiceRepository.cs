using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public class InformixServinteInvoiceRepository : IServinteInvoiceRepository
    {
        private readonly string _connectionString;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 1000;

        public InformixServinteInvoiceRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            ValidateConnectionString();
        }

        private void ValidateConnectionString()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("La cadena de conexión Informix está vacía o nula.");

            // Validar que sea una conexión ODBC válida (DSN o connection string ODBC)
            bool hasDsn = _connectionString.IndexOf("DSN=", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasDriver = _connectionString.IndexOf("Driver=", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!hasDsn && !hasDriver)
                throw new InvalidOperationException("La cadena de conexión ODBC debe contener 'DSN=' o 'Driver='.");
        }

        public async Task<List<ServinteInvoiceInfo>> GetInvoiceInfoAsync(string invoiceNumber, string eadCode)
        {
            var result = new List<ServinteInvoiceInfo>();

            try
            {
                int docNumber = Convert.ToInt32(invoiceNumber);

                int attempt = 0;
                while (attempt < MaxRetries)
                {
                    try
                    {
                        using (var connection = new OdbcConnection(_connectionString))
                        {
                            await connection.OpenAsync();

                            var query = @"
                                SELECT
                                    c.CARFUE as invoice_source,
                                    h.MOVFEC as invoice_date,
                                    h.MOVCER as agreement_code,
                                    c.CARVAL as amount,
                                    e.ENVFAECUF as electronic_code,
                                    p.EPIINAEPI as origin_event_id
                                FROM FAMOV h
                                LEFT JOIN CACAR c ON c.CARFUE = h.MOVFUE AND c.CARDOC = h.MOVDOC
                                LEFT JOIN CAENC ca ON ca.ENCFUE = h.MOVFUE AND ca.ENCDOC = h.MOVDOC AND ca.ENCSED = h.MOVEAD
                                LEFT JOIN FAENVFAE e ON e.ENVFAEFUE = h.MOVFUE AND e.ENVFAEDOC = h.MOVDOC AND e.ENVFAEEAD = h.MOVEAD AND e.ENVFAEERR = 'N'
                                LEFT JOIN HIEPIINA p ON p.EPIINAHIS = h.MOVHIS AND p.EPIINANUM = h.MOVNUM
                                WHERE h.MOVEAD = ? AND h.MOVDOC = ?
                            ";

                            using (var command = new OdbcCommand(query, connection))
                            {
                                command.CommandTimeout = 600;
                                command.Parameters.Add(new OdbcParameter("@ead", eadCode));
                                command.Parameters.Add(new OdbcParameter("@doc", docNumber));

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var info = new ServinteInvoiceInfo
                                        {
                                            InvoiceSource = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                            InvoiceDate = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
                                            AgreementCode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                            Amount = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
                                            InvoiceElectronicCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                            OriginEventId = reader.IsDBNull(5) ? string.Empty : reader.GetInt32(5).ToString()
                                        };
                                        result.Add(info);
                                    }
                                }
                            }
                        }
                        break; // Conexión exitosa, salir del loop de reintentos
                    }
                    catch (OdbcException odbcEx) when (attempt < MaxRetries - 1)
                    {
                        attempt++;
                        System.Diagnostics.Debug.WriteLine($"Intento {attempt} falló: {odbcEx.Message}. Reintentando en {RetryDelayMs}ms...");
                        await Task.Delay(RetryDelayMs);
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder errorMsg = new StringBuilder();
                errorMsg.AppendLine("Error al conectar con la base de datos Informix (ODBC):");
                errorMsg.AppendLine($"Mensaje: {ex.Message}");

                if (ex.InnerException != null)
                {
                    errorMsg.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                }

                errorMsg.AppendLine($"Cadena de conexión (parcial): {MaskConnectionString(_connectionString)}");
                errorMsg.AppendLine($"Factura: {invoiceNumber}, EAD: {eadCode}");

                System.Diagnostics.Debug.WriteLine(errorMsg.ToString());
                throw;
            }

            return result;
        }
        public async Task<List<ServinteInvoiceInfo>> GetInvoiceWithPrefix(string invoiceNumber)
        {
            var result = new List<ServinteInvoiceInfo>();

            try
            {
                int docNumber = Convert.ToInt32(invoiceNumber);

                int attempt = 0;
                while (attempt < MaxRetries)
                {
                    try
                    {
                        using (var connection = new OdbcConnection(_connectionString))
                        {
                            await connection.OpenAsync();

                            var query = @"
                                SELECT
                                    c.CARFUE as invoice_source,
                                    h.MOVDOC as invoice_number,
                                    h.MOVFEC as invoice_date,
                                    h.MOVCER as agreement_code,
                                    c.CARVAL as amount,
                                    e.ENVFAECUF as electronic_code,
                                    p.EPIINAEPI as origin_event_id
                                FROM FAMOV h
                                LEFT JOIN CACAR c ON c.CARFUE = h.MOVFUE AND c.CARDOC = h.MOVDOC
                                LEFT JOIN CAENC ca ON ca.ENCFUE = h.MOVFUE AND ca.ENCDOC = h.MOVDOC AND ca.ENCSED = h.MOVEAD
                                LEFT JOIN FAENVFAE e ON e.ENVFAEFUE = h.MOVFUE AND e.ENVFAEDOC = h.MOVDOC AND e.ENVFAEEAD = h.MOVEAD AND e.ENVFAEERR = 'N'
                                LEFT JOIN HIEPIINA p ON p.EPIINAHIS = h.MOVHIS AND p.EPIINANUM = h.MOVNUM
                                WHERE h.MOVEAD = ? AND h.MOVDOC = ?
                            ";

                            using (var command = new OdbcCommand(query, connection))
                            {
                                command.CommandTimeout = 600;
                                command.Parameters.Add(new OdbcParameter("@doc", docNumber));

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var info = new ServinteInvoiceInfo
                                        {
                                            InvoiceNumber = docNumber.ToString(),
                                            InvoiceSource = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                            InvoiceDate = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
                                            AgreementCode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                            Amount = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
                                            InvoiceElectronicCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                            OriginEventId = reader.IsDBNull(5) ? string.Empty : reader.GetInt32(5).ToString()
                                        };
                                        result.Add(info);
                                    }
                                }
                            }
                        }
                        break; // Conexión exitosa, salir del loop de reintentos
                    }
                    catch (OdbcException odbcEx) when (attempt < MaxRetries - 1)
                    {
                        attempt++;
                        System.Diagnostics.Debug.WriteLine($"Intento {attempt} falló: {odbcEx.Message}. Reintentando en {RetryDelayMs}ms...");
                        await Task.Delay(RetryDelayMs);
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder errorMsg = new StringBuilder();
                errorMsg.AppendLine("Error al conectar con la base de datos Informix (ODBC):");
                errorMsg.AppendLine($"Mensaje: {ex.Message}");

                if (ex.InnerException != null)
                {
                    errorMsg.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                }

                errorMsg.AppendLine($"Cadena de conexión (parcial): {MaskConnectionString(_connectionString)}");
                errorMsg.AppendLine($"Factura: {invoiceNumber}");

                System.Diagnostics.Debug.WriteLine(errorMsg.ToString());
                throw;
            }

            return result;
        }

        private string MaskConnectionString(string connStr)
        {
            if (string.IsNullOrEmpty(connStr))
                return "";

            // Ocultar la contraseña por seguridad
            return System.Text.RegularExpressions.Regex.Replace(
                connStr,
                @"(PWD|PASSWORD|Pwd)=([^;]+)",
                "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
