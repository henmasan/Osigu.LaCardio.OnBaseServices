using System;
using System.Collections.Generic;
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

    public class GroupedServinteInvoiceInfo
    {
        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }        
        public string InvoiceElectronicCode { get; set; }
        public List<InvoiceEvent> InvoiceEvents { get; set; }

    }

    public class InvoiceEvent{
        public string OriginEventId { get; set; }
        public string AgreementCode { get; set; }
    }

    public interface IServinteInvoiceRepository
    {
        Task<List<ServinteInvoiceInfo>> GetInvoiceInfoAsync(string invoiceNumber, string sourceCode);
    }
}
