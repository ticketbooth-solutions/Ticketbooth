using System;
using System.ComponentModel.DataAnnotations;

namespace Ticketbooth.Scanner.Data.Models
{
    public class TicketScanModel
    {
        public TicketScanModel()
        {
        }

        public TicketScanModel(string transactionHash, SeatModel seat)
        {
            TransactionHash = transactionHash;
            Seat = seat;
            Status = TicketScanStatus.Started;
        }

        [Key]
        public string TransactionHash { get; set; }

        public SeatModel Seat { get; set; }

        public TicketScanStatus Status { get; set; }

        public bool OwnsTicket { get; set; }

        public string Name { get; set; }

        public void SetScanResult(bool ownsTicket, string name)
        {
            if (name is null)
            {
                throw new ArgumentException("Name cannot be null");
            }

            OwnsTicket = ownsTicket;
            Name = name;
            Status = TicketScanStatus.Completed;
        }

        public void SetFaulted()
        {
            Status = TicketScanStatus.Faulted;
        }
    }
}
