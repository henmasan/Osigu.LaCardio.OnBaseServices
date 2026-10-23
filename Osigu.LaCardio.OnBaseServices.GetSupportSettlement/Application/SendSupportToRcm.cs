using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Osigu.OnBaseServices.GetSupportSettlement.Model.Request;
using Osigu.OnBaseServices.GetSupportSettlement.Configuration;
using System.Reflection.Metadata.Ecma335;
using Osigu.OnBaseServices.GetSupportSettlement.Model.RCMRequest;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application
{
    public class SendSupportToRcm : ISendSupportToRcm
    {
        private readonly IRcmAuthClient _rcmAuthClient;
        private readonly IRcmSupportClient _rcmSupportClient;
        private readonly IServinteInvoiceRepository _servinteRepository;
        private readonly ISupportTraceStore _traceStore;
        private readonly RcmApiSettings _rcmSettings;
        private readonly ServinteSettings _servinteSettings;
        private readonly ILogger<SendSupportToRcm> _logger;

        public SendSupportToRcm(
            IRcmAuthClient rcmAuthClient,
            IRcmSupportClient rcmSupportClient,
            IServinteInvoiceRepository servinteRepository,
            ISupportTraceStore traceStore,
            RcmApiSettings rcmSettings,
            ServinteSettings servinteSettings,
            ILogger<SendSupportToRcm> logger)
        {
            _rcmAuthClient = rcmAuthClient;
            _rcmSupportClient = rcmSupportClient;
            _servinteRepository = servinteRepository;
            _traceStore = traceStore;
            _rcmSettings = rcmSettings;
            _servinteSettings = servinteSettings;
            _logger = logger;
        }

        public async Task SendPendingSupportAsync(SupportTraceRecord trace)
        {
            List<ServinteInvoiceInfo> invoiceInfo = null;
            try
            {
                invoiceInfo = await _servinteRepository.GetInvoiceWithPrefix(trace.InvoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoice {trace.InvoiceNumber} from Servinte");
            }
            await SendPendingSupportAsync(trace, invoiceInfo);
        }

        private async Task SendPendingSupportAsync(SupportTraceRecord trace, List<ServinteInvoiceInfo> invoiceInfo)
        {
            try
            {
                _logger.LogInformation($"Processing pending support: Invoice={trace.InvoiceNumber}, Type={trace.SupportType}");

                if (trace.AttemptCount >= _rcmSettings.Retries)
                {
                    trace.Status = "Failed";
                    trace.ErrorMessage = "Maximum retry attempts exceeded";
                    trace.Observation = $"Falló: Se excedió el número máximo de reintentos ({_rcmSettings.Retries}). Última observación: {trace.ErrorMessage}";
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogError($"Max retries exceeded for {trace.InvoiceNumber}");
                    MoveSupportFileToFailed(trace.FilePath);
                    return;
                }

                if (invoiceInfo == null)
                {
                    trace.AttemptCount++;
                    trace.LastAttemptTime = DateTime.UtcNow;
                    trace.ErrorMessage = "Invoice not found in Servinte";
                    trace.Observation = $"Intento {trace.AttemptCount}: No se encontró la factura {trace.InvoiceNumber} en Servinte";
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogWarning($"Invoice {trace.InvoiceNumber} not found in Servinte");
                    return;
                }

                var token = await _rcmAuthClient.GetTokenAsync();


                var groupedInvoicesInfo = SetGroupedInvoice(invoiceInfo);

                var supportFileInfo = GetSupportFileCode(trace.SupportType);
                var fullInvoiceNumber = $"{invoiceInfo[0].InvoiceSource}{invoiceInfo[0].InvoiceNumber}";
                var supportFileMetadata = new SupportFileMetadata
                {
                    ProcessId = trace.ProcessId,
                    DocumentType = supportFileInfo.RCMDoctypeName,
                    AgreementDate = string.Empty,
                    InvoiceAmount = groupedInvoicesInfo.Amount,
                    InvoiceNumber = fullInvoiceNumber,
                    InvoiceDateTime = groupedInvoicesInfo.InvoiceDate == DateTime.MinValue ? (DateTime?)null : groupedInvoicesInfo.InvoiceDate,
                    DocumentTypeNumber = fullInvoiceNumber,
                    InvoiceElectronicCode = groupedInvoicesInfo.InvoiceElectronicCode,
                    UniqueVerificationCode = trace.UniqueVerificationCode,
                    FilePath = trace.FilePath
                };

                var uploadRequest = new RequestData();
                List<Event> events = new List<Event>();
                events = groupedInvoicesInfo.InvoiceEvents.Select(e => new Event
                {
                    AgreementCode = e.AgreementCode,
                    OriginEventId = e.OriginEventId ?? string.Empty
                }).ToList();


                uploadRequest.SupportFileCode = supportFileInfo.RCMDoctypeCode;
                uploadRequest.Events = events;
                uploadRequest.SupportFileMetadata = supportFileMetadata;


                if (string.IsNullOrEmpty(groupedInvoicesInfo.InvoiceEvents.FirstOrDefault()?.OriginEventId))
                {
                    _logger.LogWarning($"Origin event ID (episodio) not found for invoice {trace.InvoiceNumber}");
                }

                var response = await _rcmSupportClient.UploadSupportAsync(uploadRequest, token.AccessToken);

                if (response.Success)
                {
                    trace.Status = "Success";
                    trace.ErrorMessage = null;
                    trace.LastAttemptTime = DateTime.UtcNow;
                    trace.Observation = $"Exitoso: Soporte enviado a RCM. RcmId={response.RcmId}";
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogInformation($"Successfully sent {trace.SupportType} to RCM for invoice {trace.InvoiceNumber}, RcmId={response.RcmId}");
                    DeleteLocalSupportFile(trace.FilePath);
                }
                else
                {
                    trace.AttemptCount++;
                    trace.LastAttemptTime = DateTime.UtcNow;
                    trace.ErrorMessage = response.Message;
                    trace.Observation = $"Intento {trace.AttemptCount}: Fallo en RCM - {response.Message}";
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogWarning($"Failed to send {trace.SupportType} to RCM: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                trace.AttemptCount++;
                trace.LastAttemptTime = DateTime.UtcNow;
                trace.ErrorMessage = $"Error: {ex.Message}";
                trace.Observation = $"Intento {trace.AttemptCount}: Excepción - {ex.GetType().Name}: {ex.Message}";
                await _traceStore.UpdateTraceAsync(trace);
                _logger.LogError(ex, $"Error sending support to RCM for invoice {invoiceInfo[0].InvoiceNumber}");
            }
        }

        public async Task SendBatchAsync(List<SupportTraceRecord> traces)
        {
            _logger.LogInformation($"Processing batch of {traces.Count} pending traces");

            var invoiceInfoCache = new ConcurrentDictionary<string, Lazy<Task<List<ServinteInvoiceInfo>>>>();
            using var semaphore = new SemaphoreSlim(_rcmSettings.MaxConcurrentUploads);

            var pendingTasks = new List<Task>();
            foreach (var trace in traces)
            {
                pendingTasks.Add(SendTraceWithConcurrencyLimitAsync(trace, invoiceInfoCache, semaphore));
            }

            await Task.WhenAll(pendingTasks);

            _logger.LogInformation("Batch processing completed");
        }

        private async Task SendTraceWithConcurrencyLimitAsync(
            SupportTraceRecord trace,
            ConcurrentDictionary<string, Lazy<Task<List<ServinteInvoiceInfo>>>> invoiceInfoCache,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                var lazyInvoiceInfo = invoiceInfoCache.GetOrAdd(trace.InvoiceNumber, CreateLazyInvoiceInfoQuery, trace.EadCode);
                var invoiceInfo = await lazyInvoiceInfo.Value;
                await SendPendingSupportAsync(trace, invoiceInfo);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private Lazy<Task<List<ServinteInvoiceInfo>>> CreateLazyInvoiceInfoQuery(string invoiceNumber, string sourceCode)
        {
            Task<List<ServinteInvoiceInfo>> FetchInvoiceInfo() => GetInvoiceInfoSafeAsync(invoiceNumber, sourceCode);
            return new Lazy<Task<List<ServinteInvoiceInfo>>>(FetchInvoiceInfo);
        }

        private async Task<List<ServinteInvoiceInfo>> GetInvoiceInfoSafeAsync(string invoiceNumber, string sourceCode)
        {
            try
            {
                var invoiceInfo = await _servinteRepository.GetInvoiceInfoAsync(invoiceNumber, sourceCode);
                if (invoiceInfo != null)
                {
                    _logger.LogInformation($"Retrieved invoice info for {invoiceNumber}");
                }
                return invoiceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoice {invoiceNumber} from Servinte");
                return null;
            }
        }

        public async Task ProcessQueueAsync()
        {
            var pendingTraces = await _traceStore.GetPendingTracesAsync();

            if (pendingTraces.Count == 0)
            {
                _logger.LogInformation("No pending traces to process");
                return;
            }

            await SendBatchAsync(pendingTraces);
        }

        private SupportFileCodeMapping GetSupportFileCode(string supportType)
        {

            var mapping = _servinteSettings.SupportFileCodeMapping;
            SupportFileCodeMapping supportInfo = new SupportFileCodeMapping();
            supportInfo = mapping.FirstOrDefault(x => x.OnBaseDocType.Equals(supportType, StringComparison.OrdinalIgnoreCase));

            if (supportInfo != null)
            {
                return supportInfo;
            }

            _logger.LogWarning($"Support file code not found for type {supportType}, using default");
            throw new Exception($"Support file code not found for type {supportType}");
        }

        public GroupedServinteInvoiceInfo SetGroupedInvoice(List<ServinteInvoiceInfo> invoicesInfo)
        {
            var groupedInvoicesInfo = invoicesInfo
            .GroupBy(i => new { i.Amount, i.InvoiceDate, i.InvoiceElectronicCode })
            .Select(g => new GroupedServinteInvoiceInfo
            {
                Amount = g.Key.Amount,
                InvoiceDate = g.Key.InvoiceDate,
                InvoiceElectronicCode = g.Key.InvoiceElectronicCode,
                InvoiceEvents = g.Select(e => new InvoiceEvent
                {
                    OriginEventId = e.OriginEventId,
                    AgreementCode = e.AgreementCode
                }).ToList()
            }).ToList().First();

            return groupedInvoicesInfo;
        }

        private void DeleteLocalSupportFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation($"Archivo local eliminado tras envío exitoso: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar archivo local {filePath}");
            }
        }

        private void MoveSupportFileToFailed(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return;
                }

                var failedFolder = Path.Combine(Path.GetDirectoryName(filePath), "failed");
                Directory.CreateDirectory(failedFolder);

                var destination = Path.Combine(failedFolder, Path.GetFileName(filePath));
                File.Move(filePath, destination, overwrite: true);
                _logger.LogInformation($"Archivo movido a carpeta failed: {destination}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al mover archivo a carpeta failed {filePath}");
            }
        }

    }
}
