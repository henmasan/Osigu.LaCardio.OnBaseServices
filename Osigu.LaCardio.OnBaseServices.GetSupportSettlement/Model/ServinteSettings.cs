using System.Collections.Generic;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration
{
    public class ServinteSettings
    {
        public string ConnectionString { get; set; }
        public string TraceDatabasePath { get; set; }
        public int RcmSendExecutionFrequency { get; set; }
        public string RcmAgreementCode { get; set; }
        public Dictionary<string, string> SupportFileCodeMapping { get; set; }
    }
}
