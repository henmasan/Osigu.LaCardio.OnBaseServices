using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated.Model
{
    public class ProcessData
    {
       public List<Support> Support { get; set; }
    }
    public class Support
    {
        public SupportType SupportType { get; set; }
        public string SupportLocation { get; set; }
        public string SupportIndexInfo { get; set; }
        public bool SupportExist { get; set; }
    }

    public enum SupportType
    {
        FacturaXml,
        RIP,
        CUV
    }
}
