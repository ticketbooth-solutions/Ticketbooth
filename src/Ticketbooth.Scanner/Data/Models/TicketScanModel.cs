using System;

namespace Ticketbooth.Scanner.Data.Models
{
    public class TicketScanModel
    {
        public TicketScanModel()
        {
        }

        public TicketScanModel(string identifier, SeatModel seat)
        {
            Identifier = identifier;
            Seat = seat;
            Status = TicketScanStatus.Started;
            Time = DateTime.Now;
        }

        public string Identifier { get; }

        public SeatModel Seat { get; set; }

        public TicketScanStatus Status { get; set; }

        public bool OwnsTicket { get; set; }

        public string Name { get; set; }

        public DateTime Time { get; set; }

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
