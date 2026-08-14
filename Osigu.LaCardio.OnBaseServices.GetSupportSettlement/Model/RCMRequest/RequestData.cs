using Osigu.OnBaseServices.GetSupportSettlement.Model.RCMRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Osigu.OnBaseServices.GetSupportSettlement.Model.Request
{
    public class RequestData
    {

        public String SupportFileCode { get; set; }

        public List<Event> Events { get; set; }

        public SupportFileMetadata SupportFileMetadata { get; set; }
        public string? RutaFisica { get; set; }

    }
}
