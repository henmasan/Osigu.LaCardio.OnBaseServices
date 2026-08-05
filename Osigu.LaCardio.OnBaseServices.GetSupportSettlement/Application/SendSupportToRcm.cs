using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

                var invoiceInfo = await _servinteRepository.GetInvoiceInfoAsync(trace.InvoiceNumber);
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

                var uploadRequest = new RcmUploadRequest
                {
                    InvoiceNumber = trace.InvoiceNumber,
                    SupportFileCode = GetSupportFileCode(trace.SupportType),
                    AgreementCode = _servinteSettings.RcmAgreementCode,
                    DocumentType = "SUPPORT",
                    FilePath = trace.FilePath,
                    OriginEventId = Guid.NewGuid().ToString(),
                    ProcessId = Guid.NewGuid().ToString(),
                    InvoiceElectronicCode = invoiceInfo.InvoiceNumber
                };

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

            foreach (var trace in traces)
            {
                await SendPendingSupportAsync(trace);
                await Task.Delay(_rcmSettings.DelayBetweenRetriesMs);
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

        private string GetSupportFileCode(string supportType)
        {
            var mapping = _servinteSettings.SupportFileCodeMapping;
            if (mapping?.ContainsKey(supportType) == true)
            {
                return mapping[supportType];
            }

            _logger.LogWarning($"Support file code not found for type {supportType}, using default");
            return supportType;
        }
    }
}
