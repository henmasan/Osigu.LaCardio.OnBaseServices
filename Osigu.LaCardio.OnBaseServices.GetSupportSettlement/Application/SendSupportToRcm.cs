using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
                invoiceInfo = await _servinteRepository.GetInvoiceInfoAsync(trace.InvoiceNumber, trace.SourceCode);
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
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogError($"Max retries exceeded for {trace.InvoiceNumber}");
                    return;
                }

                if (invoiceInfo == null)
                {
                    trace.AttemptCount++;
                    trace.LastAttemptTime = DateTime.UtcNow;
                    trace.ErrorMessage = "Invoice not found in Servinte";
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogWarning($"Invoice {trace.InvoiceNumber} not found in Servinte");
                    return;
                }

                var token = await _rcmAuthClient.GetTokenAsync();


                var groupedInvoicesInfo = SetGroupedInvoice(invoiceInfo);

                var supportFileInfo = GetSupportFileCode(trace.SupportType);
                var fullInvoiceNumber = $"{trace.SourceCode}{trace.InvoiceNumber}";
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
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogInformation($"Successfully sent {trace.SupportType} to RCM for invoice {trace.InvoiceNumber}, RcmId={response.RcmId}");
                }
                else
                {
                    trace.AttemptCount++;
                    trace.LastAttemptTime = DateTime.UtcNow;
                    trace.ErrorMessage = response.Message;
                    await _traceStore.UpdateTraceAsync(trace);
                    _logger.LogWarning($"Failed to send {trace.SupportType} to RCM: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                trace.AttemptCount++;
                trace.LastAttemptTime = DateTime.UtcNow;
                trace.ErrorMessage = $"Error: {ex.Message}";
                await _traceStore.UpdateTraceAsync(trace);
                _logger.LogError(ex, $"Error sending support to RCM for invoice {trace.InvoiceNumber}");
            }
        }

        public async Task SendBatchAsync(List<SupportTraceRecord> traces)
        {
            _logger.LogInformation($"Processing batch of {traces.Count} pending traces");

            var groupedByInvoice = traces.GroupBy(t => t.InvoiceNumber);

            foreach (var group in groupedByInvoice)
            {
                List<ServinteInvoiceInfo> invoiceInfo = null;
                try
                {
                    invoiceInfo = await _servinteRepository.GetInvoiceInfoAsync(group.Key, group.First().SourceCode);
                    if (invoiceInfo != null)
                    {
                        _logger.LogInformation($"Retrieved invoice info for {group.Key} (processing {group.Count()} support(s))");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error retrieving invoice {group.Key} from Servinte");
                }

                foreach (var trace in group)
                {
                    await SendPendingSupportAsync(trace, invoiceInfo);
                    await Task.Delay(_rcmSettings.DelayBetweenRetriesMs);
                }
            }

            _logger.LogInformation("Batch processing completed");
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

    }
}
