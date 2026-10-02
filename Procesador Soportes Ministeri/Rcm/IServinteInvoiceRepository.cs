using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public interface IServinteInvoiceRepository
    {
        Task<List<ServinteInvoiceInfo>> GetInvoiceInfoAsync(string invoiceNumber, string eadCode);
    }
}
