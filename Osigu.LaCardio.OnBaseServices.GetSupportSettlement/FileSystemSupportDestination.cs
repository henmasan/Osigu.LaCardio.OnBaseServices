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
using Newtonsoft.Json;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure
{
    public class FileSystemSupportDestination : ISupportDestination
    {
        private readonly AppsettingConfiguration _appsetting;
        private readonly ILogger<FileSystemSupportDestination> _logger;
        private readonly ISupportTraceStore _traceStore;

        public FileSystemSupportDestination(AppsettingConfiguration appsetting, ILogger<FileSystemSupportDestination> logger, ISupportTraceStore traceStore)
        {
            _appsetting = appsetting;
            _logger = logger;
            _traceStore = traceStore;
        }

        private static bool IsCuvSupport(string supportType)
        {
            if (string.IsNullOrWhiteSpace(supportType)) return false;
            var lower = supportType.ToLowerInvariant();
            return lower.Contains("cuv") && !lower.Contains("text");
        }

        private (string? ProcessId, string? UniqueVerificationCode) TryReadCuvValidationData(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var cuv = JsonConvert.DeserializeObject<CuvValidationResult>(json);
                return (cuv?.ProcesoId.ToString(), cuv?.CodigoUnicoValidacion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"No se pudo extraer datos de validación CUV desde {filePath}");
                return (null, null);
            }
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

                    // Extract CUV validation data if this is a CUV support
                    string? cuvProcessId = null;
                    string? cuvUniqueVerificationCode = null;
                    if (IsCuvSupport(support.SupportType))
                    {
                        (cuvProcessId, cuvUniqueVerificationCode) = TryReadCuvValidationData(rcmStagingFile);
                    }

                    // Register trace record for RCM queue processing
                    var traceRecord = new SupportTraceRecord
                    {
                        InvoiceNumber = invoiceNumber,
                        SupportType = support.SupportType,
                        FilePath = rcmStagingFile,
                        Status = "Pending",
                        AttemptCount = 0,
                        ProcessId = cuvProcessId,
                        UniqueVerificationCode = cuvUniqueVerificationCode
                    };
                    await _traceStore.AddTraceAsync(traceRecord);
                    _logger.LogInformation($"Trazabilidad registrada (Pending) para soporte {support.SupportType} de factura {invoiceNumber}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar soportes");
            }
        }
    }
}
