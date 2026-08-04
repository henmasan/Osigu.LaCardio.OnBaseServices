using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model
{
    public class ProcessData
    {
       public bool Completed { get; set; }
       public List<Support> Support { get; set; }
    }
    public class Support
    {
        public string SupportType { get; set; }
        public string SupportLocation { get; set; }
        public string SupportIndexInfo { get; set; }
        public bool SupportExist { get; set; }
    }

    public enum SupportType
    {
        Factura_XML,
        RIPS,
        CUV,
        CUV_TEXT
    }
}
