using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using IBM.Data.DB2;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public class InformixServinteInvoiceRepository : IServinteInvoiceRepository
    {
        private readonly string _connectionString;

        public InformixServinteInvoiceRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<List<ServinteInvoiceInfo>> GetInvoiceInfoAsync(string invoiceNumber, string eadCode)
        {
            var result = new List<ServinteInvoiceInfo>();

            try
            {
                int docNumber = Convert.ToInt32(invoiceNumber);

                using (var connection = new DB2Connection(_connectionString))
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

                    using (var command = new DB2Command(query, connection))
                    {
                        command.CommandTimeout = 600;
                        command.Parameters.Add(new DB2Parameter("@ead", eadCode));
                        command.Parameters.Add(new DB2Parameter("@doc", docNumber));

                        using (var reader = await command.ExecuteReaderAsync() as DB2DataReader)
                        {
                            while (await reader.ReadAsync())
                            {
                                var info = new ServinteInvoiceInfo
                                {
                                    InvoiceSource = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                    InvoiceDate = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
                                    AgreementCode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    Amount = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3),
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
