using System;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Application;

namespace Ticketbooth.Scanner.ViewModels
{
    public class DetailsViewModel : INotifyPropertyChanged, IDisposable
    {
        public event EventHandler<PropertyChangedEventArgs> OnPropertyChanged;

        private readonly ITicketChecker _ticketChecker;
        private string _hash;
        private TicketScanModel _result;
        private string _messageDetail;

        public DetailsViewModel(ITicketChecker ticketChecker)
        {
            _ticketChecker = ticketChecker;
        }

        public TicketScanModel Result
        {
            get { return _result; }
            private set
            {
                _result = value;
                OnPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
            }
        }

        public string MessageDetail
        {
            get { return _messageDetail; }
            private set
            {
                _messageDetail = value;
                OnPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageDetail)));
            }
        }

        public string Icon =>
            Result is null ? "frown"
            : Result.Status == TicketScanStatus.Started ? "loader"
            : Result.Status == TicketScanStatus.Faulted ? "alert-triangle"
            : Result.OwnsTicket ? "check" : "x";

        public string Message =>
            Result is null ? "How did I get here?"
            : Result.Status == TicketScanStatus.Started ? "Processing"
            : Result.Status == TicketScanStatus.Faulted ? "Something went wrong"
            : Result.OwnsTicket ? $"Seat {Result?.Seat.Number}{Result?.Seat.Letter}" : "Invalid ticket";

        public void RetrieveTicketScan(string hash)
        {
            if (!(_hash is null))
            {
                throw new InvalidOperationException("Already retrieved ticket scan");
            }

            if (hash is null)
            {
                throw new ArgumentNullException(nameof(hash), "Hash cannot be null");
            }

            _hash = hash;

            // TODO: fetch ticket scan result

            if (Result is null)
            {
                return;
            }

            if (Result.Status == TicketScanStatus.Started)
            {
                _ticketChecker.OnCheckTicketResult += SetTicketScanResult;
            }

            SetMessageDetail();
        }

        private void SetMessageDetail()
        {
            if (Result.Status == TicketScanStatus.Faulted)
            {
                MessageDetail = "Ticket scan timed out";
            }
            else if (Result.Status == TicketScanStatus.Completed && Result.OwnsTicket)
            {
                MessageDetail = Result.Name;
            }
        }

        private void SetTicketScanResult(object sender, TicketCheckResultEventArgs ticketCheckResult)
        {
            if (ticketCheckResult.TransactionHash.Equals(_hash))
            {
                if (!ticketCheckResult.Faulted)
                {
                    Result.SetScanResult(ticketCheckResult.OwnsTicket, ticketCheckResult.Name);
                }
                else
                {
                    Result.SetFaulted();
                }

                SetMessageDetail();
                _ticketChecker.OnCheckTicketResult -= SetTicketScanResult;
            }
        }

        public void Dispose()
        {
            _ticketChecker.OnCheckTicketResult -= SetTicketScanResult;
        }
    }
}
