using System;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public class ServinteInvoiceInfo
    {
        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }
        public string AgreementCode { get; set; }
        public string InvoiceElectronicCode { get; set; }
        public string OriginEventId { get; set; }
    }

    public interface IServinteInvoiceRepository
    {
        Task<ServinteInvoiceInfo> GetInvoiceInfoAsync(string invoiceNumber);
    }
}
