using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public class OracleServinteInvoiceRepository : IServinteInvoiceRepository
    {
        private readonly string _connectionString;

        public OracleServinteInvoiceRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<List<ServinteInvoiceInfo>> GetInvoiceInfoAsync(string invoiceNumber, string eadCode)
        {
            var result = new List<ServinteInvoiceInfo>();

            try
            {
                int docNumber = Convert.ToInt32(invoiceNumber);

                using (var connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT DISTINCT
                            c.CARFUE as invoice_source,
                            h.MOVFEC as invoice_date,
                            h.MOVCER as agreement_code,
                            c.CARVAL as amount,
                            e.ENVFAECUF as electronic_code,
                            p.EPIINAEPI as origin_event_id
                        FROM FAMOV h
                        LEFT JOIN CACAR c ON c.CARFUE = h.MOVFUE AND c.CARDOC = h.MOVDOC
                        INNER JOIN CAENC ca ON ca.ENCFUE = h.MOVFUE AND ca.ENCDOC = h.MOVDOC AND ca.ENCSED = h.MOVEAD
                        LEFT JOIN FAENVFAE e ON e.ENVFAEFUE = h.MOVFUE AND e.ENVFAEDOC = h.MOVDOC AND e.ENVFAEEAD = h.MOVEAD AND e.ENVFAEERR = 'N'
                        LEFT JOIN HIEPIINA p ON p.EPIINAHIS = h.MOVHIS AND p.EPIINANUM = h.MOVNUM
                        WHERE e.ENVFAECUF IS NOT NULL AND h.MOVEAD = :ead AND h.MOVDOC = :doc
                    ";

                    using (var command = new OracleCommand(query, connection))
                    {
                        command.CommandTimeout = 600;
                        command.Parameters.Add(new OracleParameter("ead", eadCode));
                        command.Parameters.Add(new OracleParameter("doc", docNumber));

                        using (var reader = await command.ExecuteReaderAsync() as OracleDataReader)
                        {
                            while (await reader.ReadAsync())
                            {
                                var info = new ServinteInvoiceInfo
                                {
                                    InvoiceSource = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                    InvoiceDate = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
                                    AgreementCode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    Amount = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3),
                                    InvoiceElectronicCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                    OriginEventId = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                                };
                                result.Add(info);
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
    }
}
