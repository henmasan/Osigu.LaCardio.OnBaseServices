using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Osigu.OnBaseServices.GetSupportSettlement.Model.Request
{
    public class Event
{
        public string OriginEventId { get; set; }
        public string AgreementCode { get; set; }
    }
}
