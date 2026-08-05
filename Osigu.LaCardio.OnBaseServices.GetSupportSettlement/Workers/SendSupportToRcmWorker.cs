using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Workers
{
    public class SendSupportToRcmWorker : BackgroundService
    {
        private readonly ISendSupportToRcm _sendSupportToRcm;
        private readonly ServinteSettings _servinteSettings;
        private readonly ILogger<SendSupportToRcmWorker> _logger;

        public SendSupportToRcmWorker(
            ISendSupportToRcm sendSupportToRcm,
            ServinteSettings servinteSettings,
            ILogger<SendSupportToRcmWorker> logger)
        {
            _sendSupportToRcm = sendSupportToRcm;
            _servinteSettings = servinteSettings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SendSupportToRcmWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Processing RCM send queue...");
                    await _sendSupportToRcm.ProcessQueueAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing RCM send queue");
                }

                var delayMs = _servinteSettings.RcmSendExecutionFrequency > 0
                    ? _servinteSettings.RcmSendExecutionFrequency
                    : 60000;

                _logger.LogInformation($"Next RCM queue check in {delayMs / 1000} seconds");
                await Task.Delay(delayMs, stoppingToken);
            }

            _logger.LogInformation("SendSupportToRcmWorker stopped");
        }
    }
}
