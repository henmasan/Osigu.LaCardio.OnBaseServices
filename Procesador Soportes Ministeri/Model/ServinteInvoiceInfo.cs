using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorSoportesMinisterio.Model
{
    public class ServinteInvoiceInfo
    {
        public string InvoiceNumber { get; set; }
        public string InvoiceSource { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }
        public string AgreementCode { get; set; }
        public string InvoiceElectronicCode { get; set; }
        public string OriginEventId { get; set; }
    }

    public class InvoiceEvent
    {
        public string OriginEventId { get; set; }
        public string AgreementCode { get; set; }
    }

    public class GroupedServinteInvoiceInfo
    {
        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }
        public string InvoiceElectronicCode { get; set; }
        public List<InvoiceEvent> InvoiceEvents { get; set; }
    }
}
