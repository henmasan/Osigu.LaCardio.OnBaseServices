using Osigu.OnBaseServices.GetSupportSettlement.Model.RCMResponse;
using Osigu.OnBaseServices.GetSupportSettlement.Model.Request;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
   
    public interface IRcmSupportClient
    {
        Task<RCMUploadResponse> UploadSupportAsync(RequestData request, string accessToken);
    }
}
