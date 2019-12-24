using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Application
{
    public class QrCodeValidator : IQrCodeValidator
    {
        private readonly NavigationManager _navigationManager;
        private readonly ITicketChecker _ticketChecker;

        public QrCodeValidator(NavigationManager navigationManager, ITicketChecker ticketChecker)
        {
            _navigationManager = navigationManager;
            _ticketChecker = ticketChecker;
        }

        public async Task<bool> Validate(string qrCodeData)
        {
            if (string.IsNullOrWhiteSpace(qrCodeData))
            {
                return false;
            }

            try
            {
                var tickets = JsonConvert.DeserializeObject<DigitalTicket[]>(qrCodeData);
                if (tickets is null || !tickets.Any())
                {
                    return false;
                }

                var success = await CheckTickets(tickets);
                if (success)
                {
                    _navigationManager.NavigateTo("../");
                }

                return success;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        private async Task<bool> CheckTickets(params DigitalTicket[] tickets)
        {
            var ticketChecks = await Task.WhenAll(tickets.Select(ticket =>
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                return _ticketChecker.PerformTicketCheckAsync(ticket, cancellationTokenSource.Token);
            }).ToArray());
            return ticketChecks.Any(success => success);
        }
    }
}
