using System;
using System.Linq;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Application;

namespace Ticketbooth.Scanner.Eventing.Handlers
{
    public class TicketScanStateHandler : IDisposable
    {
        private readonly ITicketChecker _ticketChecker;
        private readonly ITicketRepository _ticketRepository;

        public TicketScanStateHandler(ITicketChecker ticketChecker, ITicketRepository ticketRepository)
        {
            _ticketChecker = ticketChecker;
            _ticketRepository = ticketRepository;

            _ticketChecker.OnCheckTicket += OnTicketCheck;
            _ticketChecker.OnCheckTicketResult += OnTicketCheckResult;
        }

        private void OnTicketCheck(object sender, TicketCheckRequestEventArgs ticketCheckRequest)
        {
            var seat = new SeatModel(ticketCheckRequest.Seat.Number, ticketCheckRequest.Seat.Letter);
            _ticketRepository.Add(new TicketScanModel(ticketCheckRequest.TransactionHash, seat));
        }

        private void OnTicketCheckResult(object sender, TicketCheckResultEventArgs ticketCheckResult)
        {
            var ticketScan = _ticketRepository.TicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(ticketCheckResult.TransactionHash));
            if (ticketScan is null)
            {
                return;
            }

            if (!ticketCheckResult.Faulted)
            {
                ticketScan.SetScanResult(ticketCheckResult.OwnsTicket, ticketCheckResult.Name);
            }
            else
            {
                ticketScan.SetFaulted();
            }
        }

        public void Dispose()
        {
            _ticketChecker.OnCheckTicket -= OnTicketCheck;
            _ticketChecker.OnCheckTicketResult -= OnTicketCheckResult;
        }
    }
}
