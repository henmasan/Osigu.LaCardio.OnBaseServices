using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure
{
    public class OracleServinteInvoiceRepository : IServinteInvoiceRepository
    {
        private readonly ServinteSettings _servinteSettings;
        private readonly ILogger<OracleServinteInvoiceRepository> _logger;

        public OracleServinteInvoiceRepository(ServinteSettings servinteSettings, ILogger<OracleServinteInvoiceRepository> logger)
        {
            _servinteSettings = servinteSettings;
            _logger = logger;
        }

        public async Task<ServinteInvoiceInfo> GetInvoiceInfoAsync(string invoiceNumber, string sourceCode)
        {
            try
            {
                using (var connection = new OracleConnection(_servinteSettings.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT DISTINCT
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
                            WHERE e.ENVFAECUF IS NOT NULL AND h.MOVFUE = :fue AND h.MOVDOC = :doc
                        ";

                        var fuente = new OracleParameter(":fue", sourceCode);
                        var documento = new OracleParameter(":doc", Convert.ToInt32(invoiceNumber));

                        command.Parameters.Add(fuente);
                        command.Parameters.Add(documento);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var invoiceDate = reader.IsDBNull(0) ? DateTime.MinValue : reader.GetDateTime(0);
                                var agreementCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                                var amount = reader.IsDBNull(2) ? 0m : (decimal)reader.GetDouble(2);
                                var invoiceElectronicCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                                var originEventId = reader.IsDBNull(4) ? string.Empty : reader.GetInt32(4).ToString();

                                if (string.IsNullOrEmpty(originEventId))
                                {
                                    _logger.LogWarning($"Episode (EPIINAEPI) not found for invoice {invoiceNumber}");
                                }

                                return new ServinteInvoiceInfo
                                {
                                    InvoiceDate = invoiceDate,
                                    Amount = amount,
                                    AgreementCode = agreementCode,
                                    InvoiceElectronicCode = invoiceElectronicCode,
                                    OriginEventId = originEventId
                                };
                            }
                        }
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
    }
}
