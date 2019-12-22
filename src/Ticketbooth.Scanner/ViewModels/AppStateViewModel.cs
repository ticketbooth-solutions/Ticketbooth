using System;
using System.Collections.Generic;
using System.Linq;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;

namespace Ticketbooth.Scanner.ViewModels
{
    public class AppStateViewModel : IDisposable
    {
        public event EventHandler OnDataChanged;

        private readonly List<TicketScanModel> _ticketScans;
        private readonly ITicketChecker _ticketChecker;

        public AppStateViewModel(ITicketChecker ticketChecker)
        {
            _ticketScans = new List<TicketScanModel>();
            _ticketChecker = ticketChecker;
            _ticketChecker.OnCheckTicket += AddTicketScan;
            _ticketChecker.OnCheckTicketResult += SetTicketScanResult;
        }

        public IReadOnlyList<TicketScanModel> TicketScans => _ticketScans.AsReadOnly();

        private void AddTicketScan(object sender, TicketCheckEventArgs ticketCheck)
        {
            var seat = new SeatModel(ticketCheck.Seat.Number, ticketCheck.Seat.Letter);
            _ticketScans.Add(new TicketScanModel(ticketCheck.TransactionHash, seat));
            OnDataChanged?.Invoke(sender, EventArgs.Empty);
        }

        private void SetTicketScanResult(object sender, TicketCheckResultEventArgs ticketCheckResult)
        {
            var ticketScan = _ticketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(ticketCheckResult.TransactionHash));
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

            OnDataChanged?.Invoke(sender, EventArgs.Empty);
        }

        public void Dispose()
        {
            _ticketChecker.OnCheckTicket -= AddTicketScan;
            _ticketChecker.OnCheckTicketResult -= SetTicketScanResult;
        }
    }
}
