using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

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
    }
}
