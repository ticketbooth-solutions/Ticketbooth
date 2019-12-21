using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Eventing;

namespace Ticketbooth.Scanner.Services
{
    public class QrCodeValidator
    {
        private readonly NavigationManager _navigationManager;
        private readonly ITicketChecker _ticketChecker;

        public QrCodeValidator(NavigationManager navigationManager, ITicketChecker ticketChecker)
        {
            _navigationManager = navigationManager;
            _ticketChecker = ticketChecker;
        }

        [JSInvokable]
        public async Task Validate(string qrCodeResult)
        {
            if (string.IsNullOrWhiteSpace(qrCodeResult))
            {
                return;
            }

            try
            {
                var tickets = JsonConvert.DeserializeObject<DigitalTicket[]>(qrCodeResult);
                if (tickets is null || !tickets.Any())
                {
                    return;
                }

                Task.WaitAll(tickets.Select(ticket =>
                {
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    return _ticketChecker.PerformTicketCheckAsync(ticket, cancellationTokenSource.Token);
                }).ToArray());
            }
            catch (JsonReaderException)
            {
                return;
            }

            _navigationManager.NavigateTo("../");
        }
    }
}
