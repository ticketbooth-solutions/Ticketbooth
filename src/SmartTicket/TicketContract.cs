using Stratis.SmartContracts;

public class TicketContract : SmartContract
{
    public TicketContract(ISmartContractState smartContractState, byte[] ticketsBytes, byte[] venueBytes) : base(smartContractState)
    {
        Log(Serializer.ToStruct<Venue>(venueBytes));
        PersistentState.SetAddress(nameof(Owner), Message.Sender);
        Tickets = ResetTickets(Serializer.ToArray<Ticket>(ticketsBytes));
    }

    public Ticket[] Tickets
    {
        get
        {
            return PersistentState.GetArray<Ticket>(nameof(Tickets));
        }
        private set
        {
            PersistentState.SetArray(nameof(Tickets), value);
        }
    }

    public ulong EndOfSale
    {
        get
        {
            return PersistentState.GetUInt64(nameof(EndOfSale));
        }
        private set
        {
            PersistentState.SetUInt64(nameof(EndOfSale), value);
        }
    }

    public ulong ReleaseFee
    {
        get
        {
            return PersistentState.GetUInt64(nameof(ReleaseFee));
        }
        private set
        {
            Assert(Message.Sender == Owner, "Only contract owner can set release fee");
            Assert(!SaleOpen, "Sale is open");
            PersistentState.SetUInt64(nameof(ReleaseFee), value);
        }
    }

    private bool SaleOpen
    {
        get
        {
            var endOfSale = EndOfSale;
            return endOfSale != 0 && Block.Number < endOfSale;
        }
    }

    private Address Owner
    {
        get
        {
            return PersistentState.GetAddress(nameof(Owner));
        }
    }

    public void BeginSale(byte[] ticketsBytes, byte[] showBytes)
    {
        var show = Serializer.ToStruct<Show>(showBytes);
        Assert(Message.Sender == Owner, "Only contract owner can begin a sale");
        Assert(EndOfSale == default(ulong), "Sale currently in progress");
        Assert(Block.Number < show.EndOfSale, "Sale must finish in the future");

        var tickets = Serializer.ToArray<Ticket>(ticketsBytes);
        var copyOfTickets = Tickets;

        Assert(copyOfTickets.Length == tickets.Length, "Seat elements must be equal");
        for (int i = 0; i < copyOfTickets.Length; i++)
        {
            Ticket ticket = default(Ticket);
            for (int y = 0; y < tickets.Length; y++)
            {
                if (SeatsAreEqual(copyOfTickets[i].Seat, tickets[y].Seat))
                {
                    ticket = tickets[y];
                    break;
                }
            }
            Assert(!IsDefaultSeat(ticket.Seat), "Invalid seat provided");
            copyOfTickets[i].Price = tickets[i].Price;
        }

        Tickets = copyOfTickets;
        EndOfSale = show.EndOfSale;
        Log(show);
    }

    public void EndSale()
    {
        Assert(Message.Sender == Owner, "Only contract owner can end sale");
        Assert(EndOfSale != default(ulong), "Sale not currently in progress");
        Assert(Block.Number >= EndOfSale, "Sale contract not fulfilled");

        Tickets = ResetTickets(Tickets);
        EndOfSale = default(ulong);
    }

    public bool CheckAvailability(byte[] seatIdentifierBytes)
    {
        Assert(SaleOpen, "Sale not open");

        var ticket = SelectTicket(seatIdentifierBytes);

        Assert(!IsDefaultSeat(ticket.Seat), "Seat not found");

        return IsAvailable(ticket);
    }

    public bool Reserve(byte[] seatIdentifierBytes)
    {
        if (!SaleOpen)
        {
            Refund(Message.Value);
            Assert(false, "Sale not open");
        }

        var seat = Serializer.ToStruct<Seat>(seatIdentifierBytes);
        var copyOfTickets = Tickets;
        Ticket ticket = default(Ticket);
        int ticketIndex = 0;
        for (var i = 0; i < copyOfTickets.Length; i++)
        {
            if (SeatsAreEqual(copyOfTickets[i].Seat, seat))
            {
                ticketIndex = i;
                ticket = copyOfTickets[i];
                break;
            }
        }

        // seat not found
        if (IsDefaultSeat(ticket.Seat))
        {
            Refund(Message.Value);
            Assert(false, "Seat not found");
        }

        // already reserved
        if (!IsAvailable(ticket))
        {
            Refund(Message.Value);
            return false;
        }

        // not enough funds
        if (Message.Value < ticket.Price)
        {
            Refund(Message.Value);
            return false;
        }

        if (Message.Value > ticket.Price)
        {
            Refund(Message.Value - ticket.Price);
        }

        copyOfTickets[ticketIndex].Address = Message.Sender;
        Tickets = copyOfTickets;
        return true;
    }

    public bool OwnsTicket(byte[] seatIdentifierBytes, Address address)
    {
        Assert(address != Address.Zero, "Invalid address");

        var ticket = SelectTicket(seatIdentifierBytes);

        Assert(!IsDefaultSeat(ticket.Seat), "Seat not found");

        return ticket.Address == address;
    }

    public void SetReleaseFee(ulong releaseFee)
    {
        ReleaseFee = releaseFee;
    }

    public void ReleaseTicket(byte[] seatIdentifierBytes)
    {
        Assert(SaleOpen, "Sale not open");

        var seat = Serializer.ToStruct<Seat>(seatIdentifierBytes);
        var copyOfTickets = Tickets;
        Ticket ticket = default(Ticket);
        int ticketIndex = 0;
        for (var i = 0; i < copyOfTickets.Length; i++)
        {
            if (SeatsAreEqual(copyOfTickets[i].Seat, seat))
            {
                ticketIndex = i;
                ticket = copyOfTickets[i];
                break;
            }
        }

        Assert(!IsDefaultSeat(ticket.Seat), "Seat not found");
        Assert(Message.Sender == ticket.Address, "You do not own this ticket");

        if (ticket.Price > ReleaseFee)
        {
            TryTransfer(Message.Sender, ticket.Price - ReleaseFee);
        }
        copyOfTickets[ticketIndex].Address = Address.Zero;
        Tickets = copyOfTickets;
    }

    private bool Refund(ulong amount)
    {
        Assert(amount <= Message.Value, "Invalid refund value");
        return TryTransfer(Message.Sender, amount);
    }

    private bool TryTransfer(Address recipient, ulong amount)
    {
        if (amount > 0)
        {
            var transferResult = Transfer(recipient, amount);
            return transferResult.Success;
        }

        return false;
    }

    private Ticket SelectTicket(byte[] seatIdentifierBytes)
    {
        var seat = Serializer.ToStruct<Seat>(seatIdentifierBytes);
        foreach (var ticket in Tickets)
        {
            if (SeatsAreEqual(ticket.Seat, seat))
            {
                return ticket;
            }
        }

        return default(Ticket);
    }

    private bool IsAvailable(Ticket ticket)
    {
        return ticket.Address == Address.Zero;
    }

    private bool IsDefaultSeat(Seat seat)
    {
        return seat.Number == default(int) || seat.Letter == default(char);
    }

    private bool SeatsAreEqual(Seat seat1, Seat seat2)
    {
        return seat1.Number == seat2.Number && seat1.Letter == seat2.Letter;
    }

    private Ticket[] ResetTickets(Ticket[] seats)
    {
        for (int i = 0; i < seats.Length; i++)
        {
            seats[i].Address = Address.Zero;
            seats[i].Price = 0;
        }

        return seats;
    }

    public struct Seat
    {
        public int Number;

        public char Letter;
    }

    public struct Ticket
    {
        public Seat Seat;

        public ulong Price;

        public Address Address;
    }

    public struct Venue
    {
        public string Name;
    }

    public struct Show
    {
        public string Name;

        public string Organiser;

        public ulong Time;

        public ulong EndOfSale;
    }
}