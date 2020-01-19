using System.Collections.Generic;
using System.Linq;
using Ticketbooth.Scanner.Data.Models;

namespace Ticketbooth.Scanner.Data
{
    public class TicketRepository : ITicketRepository
    {
        private const int MaxSize = 50;

        private readonly Queue<TicketScanModel> _ticketScans;

        public IReadOnlyList<TicketScanModel> TicketScans => _ticketScans.ToList().AsReadOnly();

        public TicketRepository()
        {
            _ticketScans = new Queue<TicketScanModel>();
        }

        public void Add(TicketScanModel ticketScan)
        {
            if (_ticketScans.Count == MaxSize)
            {
                _ticketScans.Dequeue();
            }

            _ticketScans.Enqueue(ticketScan);
        }

        public TicketScanModel Find(string identifier)
        {
            return _ticketScans.FirstOrDefault(ticketScan => ticketScan.Identifier.Equals(identifier));
        }
    }
}
