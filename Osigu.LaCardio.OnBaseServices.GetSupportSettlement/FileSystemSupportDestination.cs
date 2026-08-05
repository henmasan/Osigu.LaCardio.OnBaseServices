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
        private readonly AppsettingConfiguration _appsetting;
        private readonly ILogger<FileSystemSupportDestination> _logger;

        public FileSystemSupportDestination(AppsettingConfiguration appsetting, ILogger<FileSystemSupportDestination> logger)
        {
            _appsetting = appsetting;
            _logger = logger;
        }

        public async Task DeliverAsync(List<Support> supports, string invoiceNumber)
        {
            string destinationPath = _appsetting.Configuration.DestinationData.DetinationPath;
            string rcmStagingPath = _appsetting.Configuration.RcmStagingPath;

            try
            {
                foreach (var support in supports)
                {
                    FileInfo fileInfo = new FileInfo(support.SupportLocation);

                    using (StreamWriter writer = new StreamWriter($@"{destinationPath}\Support{invoiceNumber}.txt", true))
                    {
                        writer.WriteLine(support.SupportIndexInfo);
                    }

                    // Copy original to destination (keeps current behavior).
                    string destinationFile = $"{destinationPath}/{fileInfo.Name}";
                    File.Copy(support.SupportLocation, destinationFile, overwrite: true);
                    _logger.LogInformation($"Archivo copiado a: {destinationFile}");

                    // Move original to RCM staging path for later processing.
                    string rcmStagingFile = Path.Combine(rcmStagingPath, fileInfo.Name);
                    Directory.CreateDirectory(rcmStagingPath);
                    File.Move(support.SupportLocation, rcmStagingFile, overwrite: true);
                    _logger.LogInformation($"Archivo movido a staging de RCM: {rcmStagingFile}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar soportes");
            }
        }
    }
}
