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

        public async Task<ServinteInvoiceInfo> GetInvoiceInfoAsync(string invoiceNumber)
        {
            try
            {
                using (var connection = new OracleConnection(_servinteSettings.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT
                                h.NUMERO_FACTURA as invoice_number,
                                h.FECHA_EMISION as invoice_date,
                                h.VALOR_TOTAL as amount
                            FROM
                                SERVINTE.FAMOV h
                            WHERE
                                h.NUMERO_FACTURA = :invoice_number
                            AND ROWNUM = 1
                        ";

                        var param = new OracleParameter(":invoice_number", invoiceNumber);
                        command.Parameters.Add(param);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var invoiceDate = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
                                var amount = reader.IsDBNull(2) ? 0m : (decimal)reader.GetDouble(2);

                                return new ServinteInvoiceInfo
                                {
                                    InvoiceNumber = reader.GetString(0),
                                    InvoiceDate = invoiceDate,
                                    Amount = amount
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
