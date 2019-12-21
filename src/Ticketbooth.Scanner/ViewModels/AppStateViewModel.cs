using System;
using System.Collections.Generic;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;

namespace Ticketbooth.Scanner.ViewModels
{
    public class AppStateViewModel
    {
        public event EventHandler OnDataChanged;

        private readonly List<TicketScanModel> _ticketScans;

        public AppStateViewModel(ITicketChecker ticketChecker)
        {
            _ticketScans = new List<TicketScanModel>();
            ticketChecker.OnCheckTicket += AddTicketScan;
        }

        public IReadOnlyList<TicketScanModel> TicketScans => _ticketScans.AsReadOnly();

        private void AddTicketScan(object sender, TicketCheckEventArgs ticketCheck)
        {
            var seat = new SeatModel(ticketCheck.Seat.Number, ticketCheck.Seat.Letter);
            _ticketScans.Add(new TicketScanModel(ticketCheck.TransactionHash, seat));
            OnDataChanged?.Invoke(sender, EventArgs.Empty);
        }
    }
}
