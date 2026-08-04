using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public interface IDataQueries
    {
        Task<List<Invoice>> GetPendingInvoices(string status);
        Task<int> UpdateInvoice(Invoice invoice);
        Task<string> GetInvoiceNumberWithPrefix(string invoiceNumber);
    }
}
