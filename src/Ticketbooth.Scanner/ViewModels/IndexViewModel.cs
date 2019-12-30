using System;
using System.Collections.Generic;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Application;

namespace Ticketbooth.Scanner.ViewModels
{
    public class IndexViewModel : INotifyPropertyChanged, IDisposable
    {
        public event EventHandler<PropertyChangedEventArgs> OnPropertyChanged;

        private readonly ITicketRepository _ticketRepository;
        private readonly ITicketChecker _ticketChecker;

        public IndexViewModel(ITicketRepository ticketRepository, ITicketChecker ticketChecker)
        {
            _ticketRepository = ticketRepository;
            _ticketChecker = ticketChecker;
            _ticketChecker.OnCheckTicket += OnTicketScanEvent;
            _ticketChecker.OnCheckTicketResult += OnTicketScanEvent;
        }

        public IReadOnlyList<TicketScanModel> TicketScans => _ticketRepository.TicketScans;

        private void OnTicketScanEvent(object sender, EventArgs e)
        {
            OnPropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(TicketScans)));
        }

        public void Dispose()
        {
            _ticketChecker.OnCheckTicket -= OnTicketScanEvent;
            _ticketChecker.OnCheckTicketResult -= OnTicketScanEvent;
        }
    }
}
