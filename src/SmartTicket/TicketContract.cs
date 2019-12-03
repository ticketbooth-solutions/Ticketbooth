using Stratis.SmartContracts;

public class TicketContract : SmartContract
{
    private Address Owner
    {
        get
        {
            return PersistentState.GetAddress(nameof(Owner));
        }
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

    public TicketContract(ISmartContractState smartContractState, byte[] ticketsBytes) : base(smartContractState)
    {
        PersistentState.SetAddress(nameof(Owner), Message.Sender);
        Tickets = ResetTickets(Serializer.ToArray<Ticket>(ticketsBytes));
    }

    public void BeginSale(byte[] ticketsBytes, ulong endOfSale)
    {
        Assert(Message.Sender == Owner, "Only contract owner can begin a sale");
        Assert(EndOfSale == default(ulong), "Sale currently in progress");
        Assert(Block.Number < endOfSale, "Sale must finish in the future");

        var tickets = Serializer.ToArray<Ticket>(ticketsBytes);
        var copyOfTickets = Tickets;

        Assert(copyOfTickets.Length == tickets.Length, "Seat elements must be equal");
        for (int i = 0; i < copyOfTickets.Length; i++)
        {
            Ticket ticket = default(Ticket);
            for (int y = 0; y < tickets.Length; y++)
            {
                if (copyOfTickets[i].Seat.Number == tickets[y].Seat.Number && copyOfTickets[i].Seat.Letter == tickets[y].Seat.Letter)
                {
                    ticket = tickets[y];
                    break;
                }
            }
            Assert(!IsDefaultSeat(ticket.Seat), "Invalid seat provided");
            copyOfTickets[i].Price = tickets[i].Price;
        }

        Tickets = copyOfTickets;
        EndOfSale = endOfSale;
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
        Assert(EndOfSale != 0 && Block.Number < EndOfSale, "Sale not in progress");

        var ticket = SelectTicket(seatIdentifierBytes);

        Assert(!IsDefaultSeat(ticket.Seat), "Seat not found");

        return IsAvailable(ticket);
    }

    public bool Reserve(byte[] seatIdentifierBytes)
    {
        Assert(EndOfSale != 0 && Block.Number < EndOfSale, "Sale not in progress");

        var seat = Serializer.ToStruct<Seat>(seatIdentifierBytes);
        var copyOfTickets = Tickets;
        Ticket ticket = default(Ticket);
        int ticketIndex = 0;
        for (var i = 0; i < copyOfTickets.Length; i++)
        {
            if (copyOfTickets[i].Seat.Number == seat.Number && copyOfTickets[i].Seat.Letter == seat.Letter)
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

    private bool Refund(ulong amount)
    {
        Assert(amount <= Message.Value, "Invalid refund value");
        if (amount > 0)
        {
            var transferResult = Transfer(Message.Sender, amount);
            return transferResult.Success;
        }

        return false;
    }

    private Ticket SelectTicket(byte[] seatIdentifierBytes)
    {
        var seat = Serializer.ToStruct<Seat>(seatIdentifierBytes);
        foreach (var ticket in Tickets)
        {
            if (ticket.Seat.Number == seat.Number && ticket.Seat.Letter == seat.Letter)
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
}