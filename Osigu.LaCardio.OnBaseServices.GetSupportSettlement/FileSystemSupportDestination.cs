using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure
{
    public class FileSystemSupportDestination : ISupportDestination
    {
        private readonly Configuration _appsetting;
        private readonly ILogger<FileSystemSupportDestination> _logger;

        public FileSystemSupportDestination(Configuration appsetting, ILogger<FileSystemSupportDestination> logger)
        {
            _appsetting = appsetting;
            _logger = logger;
        }

        public async Task DeliverAsync(List<Support> supports, string invoiceNumber)
        {
            string destinationPath = _appsetting.DestinationData.DetinationPath;
            try
            {
                foreach (var support in supports)
                {
                    using (StreamWriter writer = new StreamWriter($@"{destinationPath}\Support{invoiceNumber}.txt", true))
                    {
                        writer.WriteLine(support.SupportIndexInfo);
                    }
                    FileInfo fileInfo = new FileInfo(support.SupportLocation);

                    File.Move(support.SupportLocation, $"{destinationPath}/{fileInfo.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mover soportes");
            }
        }
    }
}
