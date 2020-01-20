using System.Collections.Generic;
using Ticketbooth.Scanner.Data.Models;

namespace Ticketbooth.Scanner.Data
{
    public interface ITicketRepository
    {
        IReadOnlyList<TicketScanModel> TicketScans { get; }

        void Add(TicketScanModel ticketScan);

        TicketScanModel Find(string identifier);
    }
}
