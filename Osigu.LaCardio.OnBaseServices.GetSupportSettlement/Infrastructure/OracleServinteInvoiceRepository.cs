using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure
{
    public class OracleServinteInvoiceRepository : IServinteInvoiceRepository
    {
        private readonly ServinteSettings _servinteSettings;
        private readonly ILogger<OracleServinteInvoiceRepository> _logger;
        private AppsettingConfiguration _appsetting;



        public OracleServinteInvoiceRepository(ServinteSettings servinteSettings, ILogger<OracleServinteInvoiceRepository> logger, AppsettingConfiguration appsetting)
        {
            _servinteSettings = servinteSettings;
            _logger = logger;
            _appsetting = appsetting;
        }

        public async Task<List<ServinteInvoiceInfo>> GetInvoiceInfoAsync(string invoiceNumber, string eadCode)
        {
            try
            {
                using (var connection = new OracleConnection(_servinteSettings.ConnectionString))
                {
                    await connection.OpenAsync();

                    List<ServinteInvoiceInfo> invoiceInfos = new List<ServinteInvoiceInfo>();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT DISTINCT
                                c.CARFUE        AS invoice_source,
                                h.MOVFEC        AS invoice_date,
                                h.MOVCER        AS agreement_code,
                                c.CARVAL        AS amount,
                                e.ENVFAECUF     AS invoice_electronic_code,
                                p.EPIINAEPI     AS episode_number
                            FROM FAMOV h
                            LEFT JOIN CACAR c
                                ON c.CARFUE = h.MOVFUE AND c.CARDOC = h.MOVDOC
                            INNER JOIN CAENC ca
                                ON ca.ENCFUE = h.MOVFUE AND ca.ENCDOC = h.MOVDOC AND ca.ENCSED = h.MOVEAD
                            LEFT JOIN FAENVFAE e
                                ON e.ENVFAEFUE = h.MOVFUE AND e.ENVFAEDOC = h.MOVDOC AND e.ENVFAEEAD = h.MOVEAD AND e.ENVFAEERR = 'N'
                            LEFT JOIN HIEPIINA p
                                ON p.EPIINAHIS = h.MOVHIS AND p.EPIINANUM = h.MOVNUM
                            WHERE e.ENVFAECUF IS NOT NULL AND h.MOVEAD = :ead AND h.MOVDOC = :doc
                        ";

                        var ead = new OracleParameter(":ead", eadCode);
                        var documento = new OracleParameter(":doc", Convert.ToInt32(invoiceNumber));

                        command.Parameters.Add(ead);
                        command.Parameters.Add(documento);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var invoiceSource = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                                var invoiceDate = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
                                var agreementCode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                                var amount = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3);
                                var invoiceElectronicCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                                var originEventId = reader.IsDBNull(5) ? string.Empty : reader.GetInt32(5).ToString();

                                if (string.IsNullOrEmpty(originEventId))
                                {
                                    _logger.LogWarning($"Episode (EPIINAEPI) not found for invoice {invoiceNumber}");
                                }

                                ServinteInvoiceInfo invoiceInfo = new ServinteInvoiceInfo();

                                invoiceInfo.InvoiceSource = invoiceSource;
                                invoiceInfo.InvoiceDate = invoiceDate;
                                invoiceInfo.Amount = amount;
                                invoiceInfo.AgreementCode = agreementCode;
                                invoiceInfo.InvoiceElectronicCode = invoiceElectronicCode;
                                invoiceInfo.OriginEventId = originEventId;

                                invoiceInfos.Add(invoiceInfo);
                            }
                        }
                        return invoiceInfos;
                    }
                }

                _logger.LogWarning($"Invoice {invoiceNumber} not found in Servinte");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoice {invoiceNumber} from Servinte");
                throw;
            }
        }

        public async Task<List<ServinteInvoiceInfo>> GetInvoiceWithPrefix(string invoiceNumber)
        {
            var result = new List<ServinteInvoiceInfo>();

            try
            {
                int docNumber = Convert.ToInt32(invoiceNumber);

                int attempt = 0;
                while (attempt < _appsetting.RcmApi.MaxRetries)
                {
                    try
                    {
                        using (var connection = new OracleConnection(_servinteSettings.ConnectionString))
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

                            using (var command = connection.CreateCommand())
                            {
                                command.CommandTimeout = 600;
                                command.Parameters.Add(new OracleParameter("@doc", docNumber));

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
                    catch (OracleException oracleEx) when (attempt < _appsetting.RcmApi.MaxRetries - 1)
                    {
                        attempt++;
                        System.Diagnostics.Debug.WriteLine($"Intento {attempt} falló: {oracleEx.Message}. Reintentando en {_appsetting.RcmApi.RetriesDelay}ms...");
                        await Task.Delay(_appsetting.RcmApi.RetriesDelay);
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

                errorMsg.AppendLine($"Cadena de conexión (parcial): {MaskConnectionString(_servinteSettings.ConnectionString)}");
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

