using System;
using System.Collections.Generic;
using System.Linq;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Application;

namespace Ticketbooth.Scanner.ViewModels
{
    public class AppStateViewModel : INotifyPropertyChanged, IDisposable
    {
        public event EventHandler<PropertyChangedEventArgs> OnPropertyChanged;

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
            OnPropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(TicketScans)));
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

            OnPropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(TicketScans)));
        }

        public void Dispose()
        {
            _ticketChecker.OnCheckTicket -= AddTicketScan;
            _ticketChecker.OnCheckTicketResult -= SetTicketScanResult;
        }
    }
}
