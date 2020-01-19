using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Requests;

namespace Ticketbooth.Scanner.Services.Application
{
    public class QrCodeValidator : IQrCodeValidator
    {
        public event EventHandler OnValidQrCode;

        private readonly ILogger<QrCodeValidator> _logger;
        private readonly IMediator _mediator;

        public QrCodeValidator(ILogger<QrCodeValidator> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Validate(string qrCodeData)
        {
            if (string.IsNullOrWhiteSpace(qrCodeData))
            {
                _logger.LogDebug("QR code data empty");
                return;
            }

            try
            {
                var tickets = JsonConvert.DeserializeObject<DigitalTicket[]>(qrCodeData);
                if (tickets is null || !tickets.Any())
                {
                    _logger.LogDebug("No tickets specified");
                    return;
                }

                OnValidQrCode?.Invoke(this, null);
                _logger.LogInformation("Begun processing ticket check transacions");
                await _mediator.Send(new TicketScanRequest(tickets));
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e.Message);
            }
            catch (FormatException e)
            {
                _logger.LogWarning(e.Message);
            }
        }
    }
}
