using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Notifications;

namespace Ticketbooth.Scanner.Services.Application
{
    public class QrCodeValidator : IQrCodeValidator
    {
        public event EventHandler OnValidQrCode;

        private readonly ILogger<QrCodeValidator> _logger;
        private readonly IMediator _mediator;
        private readonly ITicketChecker _ticketChecker;

        public QrCodeValidator(ILogger<QrCodeValidator> logger, IMediator mediator, ITicketChecker ticketChecker)
        {
            _logger = logger;
            _mediator = mediator;
            _ticketChecker = ticketChecker;
        }

        public async Task Validate(string qrCodeData)
        {
            if (string.IsNullOrWhiteSpace(qrCodeData))
            {
                return;
            }

            try
            {
                var tickets = JsonConvert.DeserializeObject<DigitalTicket[]>(qrCodeData);
                if (tickets is null || !tickets.Any())
                {
                    return;
                }

                var ticketCheckTransactions = new Dictionary<string, DigitalTicket>();
                var ticketChecks = await Task.WhenAll(tickets.Select(async ticket =>
                {
                    var result = await _ticketChecker.PerformTicketCheckAsync(ticket);
                    if (!(result is null))
                    {
                        ticketCheckTransactions.Add(result, ticket);
                    }
                    return result;
                }).ToArray());

                if (ticketCheckTransactions.Any())
                {
                    OnValidQrCode?.Invoke(this, null);
                    _logger.LogInformation("Begun processing ticket check transacions");
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await Task.WhenAll(ticketCheckTransactions
                        .Select(transaction => _mediator.Publish(
                            new TicketScanStartedNotification(transaction.Key, transaction.Value.Seat), cancellationTokenSource.Token)));
                }
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e.Message);
                return;
            }
        }
    }
}
