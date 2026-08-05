using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System.Collections.Generic;
using System.IO;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public interface ISupportClassifier
    {
        Support ClassifySupport(FileInfo fileInfo, string invoiceNumber, List<FileParameters> fileParameters);
    }
}
