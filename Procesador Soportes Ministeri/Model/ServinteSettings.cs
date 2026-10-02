using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorSoportesMinisterio.Model
{
    public class ServinteSettings
    {
        public string DatabaseOrigin { get; set; }
        public string ConnectionString { get; set; }
        public string InformixConnectionString { get; set; }
        public string TraceDatabasePath { get; set; }
        public int RcmSendExecutionFrequency { get; set; }
        public List<SupportFileCodeMapping> SupportFileCodeMapping { get; set; }
    }

    public class SupportFileCodeMapping
    {
        public string OnBaseDocType { get; set; }
        public string RCMDoctypeCode { get; set; }
        public string RCMDoctypeName { get; set; }
    }
}
