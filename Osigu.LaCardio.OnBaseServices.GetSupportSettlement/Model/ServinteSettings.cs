using Osigu.OnBaseServices.GetSupportSettlement.Configuration;
using System.Collections.Generic;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration
{
    public class ServinteSettings
    {
        public string ConnectionString { get; set; }
        public string TraceDatabasePath { get; set; }
        public int RcmSendExecutionFrequency { get; set; }
        public List<SupportFileCodeMapping> SupportFileCodeMapping { get; set; }
    }
}
