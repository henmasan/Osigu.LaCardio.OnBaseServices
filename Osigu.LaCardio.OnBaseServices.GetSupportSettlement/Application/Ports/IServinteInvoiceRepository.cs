using System;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public class ServinteInvoiceInfo
    {
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }
    }

    public interface IServinteInvoiceRepository
    {
        Task<ServinteInvoiceInfo> GetInvoiceInfoAsync(string invoiceNumber);
    }
}
