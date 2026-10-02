using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public interface ISendSupportToRcm
    {
        Task ProcessQueueAsync();
    }

    public class SendSupportToRcm : ISendSupportToRcm
    {
        private readonly ISupportTraceStore _traceStore;
        private readonly IServinteInvoiceRepository _servinteRepository;
        private readonly IRcmAuthClient _rcmAuthClient;
        private readonly IRcmSupportClient _rcmSupportClient;
        private readonly RcmApiSettings _rcmSettings;
        private readonly ServinteSettings _servinteSettings;
        private readonly string _rcmStagingPath;

        public SendSupportToRcm(
            ISupportTraceStore traceStore,
            IServinteInvoiceRepository servinteRepository,
            IRcmAuthClient rcmAuthClient,
            IRcmSupportClient rcmSupportClient,
            RcmApiSettings rcmSettings,
            ServinteSettings servinteSettings,
            string rcmStagingPath)
        {
            _traceStore = traceStore ?? throw new ArgumentNullException(nameof(traceStore));
            _servinteRepository = servinteRepository ?? throw new ArgumentNullException(nameof(servinteRepository));
            _rcmAuthClient = rcmAuthClient ?? throw new ArgumentNullException(nameof(rcmAuthClient));
            _rcmSupportClient = rcmSupportClient ?? throw new ArgumentNullException(nameof(rcmSupportClient));
            _rcmSettings = rcmSettings ?? throw new ArgumentNullException(nameof(rcmSettings));
            _servinteSettings = servinteSettings ?? throw new ArgumentNullException(nameof(servinteSettings));
            _rcmStagingPath = rcmStagingPath ?? throw new ArgumentNullException(nameof(rcmStagingPath));
        }

        public async Task ProcessQueueAsync()
        {
            try
            {
                var pendingTraces = await _traceStore.GetPendingTracesAsync();
                if (pendingTraces.Count == 0)
                {
                    return;
                }

                await SendBatchAsync(pendingTraces);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task SendBatchAsync(List<SupportTraceRecord> traces)
        {
            var cache = new ConcurrentDictionary<string, Lazy<Task<List<ServinteInvoiceInfo>>>>();
            var semaphore = new SemaphoreSlim(_rcmSettings.MaxConcurrentUploads, _rcmSettings.MaxConcurrentUploads);

            try
            {
                var tasks = traces.Select(async trace =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var cacheKey = $"{trace.EadCode}_{trace.InvoiceNumber}";
                        var lazyTask = cache.GetOrAdd(cacheKey,
                            key => new Lazy<Task<List<ServinteInvoiceInfo>>>(
                                () => _servinteRepository.GetInvoiceInfoAsync(trace.InvoiceNumber, trace.EadCode)
                            )
                        );

                        var invoiceInfo = await lazyTask.Value;
                        await SendPendingSupportAsync(trace, invoiceInfo);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task SendPendingSupportAsync(SupportTraceRecord trace, List<ServinteInvoiceInfo> invoiceInfoList)
        {
            try
            {
                if (trace.AttemptCount >= _rcmSettings.Retries)
                {
                    trace.Status = "Failed";
                    trace.ErrorMessage = "Maximum retry attempts exceeded";
                    trace.Observation = $"Failed after {trace.AttemptCount} attempts";
                    await _traceStore.UpdateTraceAsync(trace);
                    MoveSupportFileToFailed(trace.FilePath);
                    return;
                }

                if (invoiceInfoList == null || invoiceInfoList.Count == 0)
                {
                    trace.AttemptCount++;
                    trace.ErrorMessage = "Invoice not found in Servinte";
                    trace.LastAttemptTime = DateTime.UtcNow;
                    await _traceStore.UpdateTraceAsync(trace);
                    return;
                }

                var token = await _rcmAuthClient.GetTokenAsync();
                var supportCodeMapping = _servinteSettings.SupportFileCodeMapping
                    .FirstOrDefault(x => x.OnBaseDocType.Equals(trace.SupportType, StringComparison.OrdinalIgnoreCase));

                if (supportCodeMapping == null)
                {
                    trace.AttemptCount++;
                    trace.ErrorMessage = $"No RCM mapping found for support type: {trace.SupportType}";
                    trace.LastAttemptTime = DateTime.UtcNow;
                    await _traceStore.UpdateTraceAsync(trace);
                    return;
                }

                var groupedInvoices = invoiceInfoList
                    .GroupBy(x => new { x.InvoiceDate, x.Amount, x.InvoiceElectronicCode })
                    .FirstOrDefault();

                if (groupedInvoices == null)
                {
                    trace.AttemptCount++;
                    trace.ErrorMessage = "No grouped invoice info found";
                    trace.LastAttemptTime = DateTime.UtcNow;
                    await _traceStore.UpdateTraceAsync(trace);
                    return;
                }

                var firstInvoice = groupedInvoices.First();
                var fullInvoiceNumber = $"{trace.EadCode}{trace.InvoiceNumber}";
                var events = groupedInvoices
                    .Select(x => new EventInfo
                    {
                        OriginEventId = x.OriginEventId ?? "",
                        AgreementCode = x.AgreementCode ?? ""
                    })
                    .ToList();

                if (string.IsNullOrEmpty(events[0].OriginEventId))
                {
                    // Log warning but continue
                }

                var metadata = new SupportFileMetadata
                {
                    ProcessId = trace.ProcessId,
                    DocumentType = supportCodeMapping.RCMDoctypeName,
                    AgreementDate = "",
                    InvoiceAmount = firstInvoice.Amount,
                    InvoiceNumber = fullInvoiceNumber,
                    InvoiceDateTime = firstInvoice.InvoiceDate == DateTime.MinValue ? (DateTime?)null : firstInvoice.InvoiceDate,
                    DocumentTypeNumber = fullInvoiceNumber,
                    InvoiceElectronicCode = firstInvoice.InvoiceElectronicCode,
                    UniqueVerificationCode = trace.UniqueVerificationCode
                };

                var requestData = new RequestData
                {
                    SupportFileCode = supportCodeMapping.RCMDoctypeCode,
                    Events = events,
                    SupportFileMetadata = metadata,
                    RutaFisica = null
                };

                // Store the file path in metadata temporarily for upload
                requestData.SupportFileMetadata.InvoiceNumber = trace.FilePath;

                var uploadResult = await _rcmSupportClient.UploadSupportAsync(requestData, token.AccessToken);

                if (uploadResult.Success)
                {
                    trace.Status = "Success";
                    trace.ErrorMessage = null;
                    trace.Observation = $"Exitoso: Soporte enviado a RCM. RcmId={uploadResult.RcmId}";
                    trace.LastAttemptTime = DateTime.UtcNow;
                    await _traceStore.UpdateTraceAsync(trace);
                    DeleteLocalSupportFile(trace.FilePath);
                }
                else
                {
                    trace.AttemptCount++;
                    trace.ErrorMessage = uploadResult.ErrorMessage;
                    trace.Observation = uploadResult.ErrorMessage;
                    trace.LastAttemptTime = DateTime.UtcNow;
                    await _traceStore.UpdateTraceAsync(trace);
                }
            }
            catch (Exception ex)
            {
                trace.AttemptCount++;
                trace.ErrorMessage = $"Error: {ex.Message}";
                trace.Observation = $"Excepción - {ex.GetType().Name}";
                trace.LastAttemptTime = DateTime.UtcNow;

                try
                {
                    await _traceStore.UpdateTraceAsync(trace);
                }
                catch
                {
                    // Log if possible
                }
            }
        }

        private void MoveSupportFileToFailed(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                var failedDir = Path.Combine(Path.GetDirectoryName(filePath), "failed");
                if (!Directory.Exists(failedDir))
                {
                    Directory.CreateDirectory(failedDir);
                }

                var failedFilePath = Path.Combine(failedDir, Path.GetFileName(filePath));
                File.Move(filePath, failedFilePath, true);
            }
            catch
            {
                // Log if possible
            }
        }

        private void DeleteLocalSupportFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Log if possible
            }
        }
    }
}
