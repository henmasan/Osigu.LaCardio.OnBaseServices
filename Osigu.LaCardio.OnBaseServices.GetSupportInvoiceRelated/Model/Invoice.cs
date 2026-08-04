using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated.Model
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceWithNumber { get; set; }
        public string InvoicePrefix { get; set; }
        public string InvoiceNumber{ get; set; }
        public DateTime RegistryDate{ get; set; }
        public string Status { get; set; }
        public string ProcessData { get; set; }
    }
}
