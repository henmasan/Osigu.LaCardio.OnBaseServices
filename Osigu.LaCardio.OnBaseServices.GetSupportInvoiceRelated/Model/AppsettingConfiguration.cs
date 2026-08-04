using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated.Model
{
    public class AppsettingConfiguration
    {
        public Configuration Configuration { get; set; }
    }
    public class Configuration
    {
      public string ConnectionString { get; set; }
      public string SupportPath { get; set; }
    }
}
