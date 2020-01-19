using Easy.MessageHub;
using System;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Eventing.Events;

namespace Ticketbooth.Scanner.ViewModels
{
    public class DetailsViewModel : INotifyPropertyChanged, IDisposable
    {
        public event EventHandler<PropertyChangedEventArgs> OnPropertyChanged;

        private readonly IMessageHub _eventAggregator;
        private readonly ITicketRepository _ticketRepository;

        private string _identifier;
        private Guid _ticketScanUpdatedSubscription;

        public DetailsViewModel(IMessageHub eventAggregator, ITicketRepository ticketRepository)
        {
            _eventAggregator = eventAggregator;
            _ticketRepository = ticketRepository;
        }

        public TicketScanModel Result => _ticketRepository.Find(_identifier);

        public string Message =>
            Result is null ? "How did I get here?"
            : Result.Status == TicketScanStatus.Started ? "Processing"
            : Result.Status == TicketScanStatus.Faulted ? "Something went wrong"
            : Result.OwnsTicket ? $"Seat {Result?.Seat.Number}{Result?.Seat.Letter}" : "Invalid ticket";

        public string MessageDetail =>
            Result?.Status == TicketScanStatus.Faulted ? "Invalid ticket data"
            : Result?.Status == TicketScanStatus.Completed && Result.OwnsTicket ? Result.Name
            : null;

        public void RetrieveTicketScan(string identifier)
        {
            if (!(_identifier is null))
            {
                throw new InvalidOperationException("Already retrieved ticket scan");
            }

            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier), "Identifier cannot be null");
            }

            _identifier = identifier;

            if (Result?.Status == TicketScanStatus.Started)
            {
                _ticketScanUpdatedSubscription = _eventAggregator.Subscribe<TicketScanUpdated>(
                    message => OnPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message))));
            }
        }

        public void Dispose()
        {
            _eventAggregator.Unsubscribe(_ticketScanUpdatedSubscription);
        }
    }
}
