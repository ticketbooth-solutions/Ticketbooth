﻿using Stratis.SmartContracts;

public class TicketContract : SmartContract
{
    /// <summary>
    /// Creates a new ticketing contract
    /// </summary>
    /// <param name="smartContractState"></param>
    /// <param name="seatsBytes">The serialized array of seats</param>
    /// <param name="venueName">The venue that hosts the contract</param>
    public TicketContract(ISmartContractState smartContractState, byte[] seatsBytes, string venueName) : base(smartContractState)
    {
        var seats = Serializer.ToArray<Seat>(seatsBytes);
        var tickets = new Ticket[seats.Length];

        for (int i = 0; i < seats.Length; i++)
        {
            // assert uniqueness
            var next = i + 1;
            for (int j = next; j < seats.Length; j++)
            {
                Assert(!SeatsAreEqual(seats[i], seats[j]), "Seats must all be unique");
            }

            tickets[i] = new Ticket { Seat = seats[i] };
        }

        Log(new Venue { Name = venueName });
        PersistentState.SetAddress(nameof(Owner), Message.Sender);
        Tickets = tickets;
    }

    /// <summary>
    /// Stores ticket data for the contract
    /// </summary>
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

    /// <summary>
    /// The block at which the sale ends
    /// </summary>
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

    /// <summary>
    /// The cost of refunding a ticket after purchase
    /// </summary>
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

    /// <summary>
    /// The number of blocks before the end of the sale where refunds are not issued
    /// </summary>
    public ulong NoRefundBlocks
    {
        get
        {
            return PersistentState.GetUInt64(nameof(NoRefundBlocks));
        }
        private set
        {
            Assert(Message.Sender == Owner, "Only contract owner can set refund block lock limit");
            Assert(!SaleOpen, "Sale is open");
            PersistentState.SetUInt64(nameof(NoRefundBlocks), value);
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

    /// <summary>
    /// Starts a ticket sale, when no sale is running
    /// </summary>
    /// <param name="ticketsBytes">The serialized array of tickets</param>
    /// <param name="showName">Name of the event or performance</param>
    /// <param name="organiser">The organiser or artist</param>
    /// <param name="time">Unix time for the event</param>
    /// <param name="endOfSale">The block at which the sale ends</param>
    public void BeginSale(byte[] ticketsBytes, string showName, string organiser, ulong time, ulong endOfSale)
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
        EndOfSale = endOfSale;

        var show = new Show
        {
            Name = showName,
            Organiser = organiser,
            Time = time,
            EndOfSale = endOfSale
        };
        Log(show);
    }

    /// <summary>
    /// Called after the ending of a ticket sale to clear the contract ticket data
    /// </summary>
    public void EndSale()
    {
        Assert(Message.Sender == Owner, "Only contract owner can end sale");
        Assert(EndOfSale != default(ulong), "Sale not currently in progress");
        Assert(Block.Number >= EndOfSale, "Sale contract not fulfilled");

        Tickets = ResetTickets(Tickets);
        EndOfSale = default(ulong);
    }

    /// <summary>
    /// Checks the availability of a seat
    /// </summary>
    /// <param name="seatIdentifierBytes">The serialized seat identifier</param>
    /// <returns>Whether the seat is available</returns>
    public bool CheckAvailability(byte[] seatIdentifierBytes)
    {
        Assert(SaleOpen, "Sale not open");

        var ticket = SelectTicket(seatIdentifierBytes);

        Assert(!IsDefaultSeat(ticket.Seat), "Seat not found");

        return IsAvailable(ticket);
    }

    /// <summary>
    /// Reserves a ticket for the callers address
    /// </summary>
    /// <param name="seatIdentifierBytes">The serialized seat identifier</param>
    /// <returns>Whether the seat was successfully reserved</returns>
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

    /// <summary>
    /// Used to verify whether an address owns a ticket
    /// </summary>
    /// <param name="seatIdentifierBytes">The serialized seat identifier</param>
    /// <param name="address">The address to verify</param>
    /// <returns>Whether verification is successful</returns>
    public bool OwnsTicket(byte[] seatIdentifierBytes, Address address)
    {
        Assert(address != Address.Zero, "Invalid address");

        var ticket = SelectTicket(seatIdentifierBytes);

        Assert(!IsDefaultSeat(ticket.Seat), "Seat not found");

        return ticket.Address == address;
    }

    /// <summary>
    /// Sets the fee to refund a ticket to the contract
    /// </summary>
    /// <param name="releaseFee">The refund fee</param>
    public void SetReleaseFee(ulong releaseFee)
    {
        ReleaseFee = releaseFee;
    }

    /// <summary>
    /// Sets the block limit for issuing refunds on purchased tickets
    /// </summary>
    /// <param name="noRefundBlocks">The number of blocks before the end of the contract to disallow refunds</param>
    public void SetNoRefundBlocks(ulong noRefundBlocks)
    {
        NoRefundBlocks = noRefundBlocks;
    }

    /// <summary>
    /// Requests a refund for a ticket, which will be issued if the <see cref="NoRefundBlocks" /> limit is not yet reached
    /// </summary>
    /// <param name="seatIdentifierBytes">The serialized seat identifier</param>
    public void ReleaseTicket(byte[] seatIdentifierBytes)
    {
        Assert(SaleOpen, "Sale not open");
        Assert(Block.Number + NoRefundBlocks < EndOfSale, "Surpassed no refund block limit");

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

    /// <summary>
    /// Identifies a specific seat by number and/or letter
    /// </summary>
    public struct Seat
    {
        public int Number;

        public char Letter;
    }

    /// <summary>
    /// Represents a ticket for a specific seat
    /// </summary>
    public struct Ticket
    {
        /// <summary>
        /// The seat the ticket is for
        /// </summary>
        public Seat Seat;

        /// <summary>
        /// Price of the ticket in CRS sats
        /// </summary>
        public ulong Price;

        /// <summary>
        /// The ticket owner
        /// </summary>
        public Address Address;
    }

    /// <summary>
    /// Stores metadata about the ticketing contract
    /// </summary>
    public struct Venue
    {
        /// <summary>
        /// Name of the venue
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Stores metadata relating to a specific ticket sale
    /// </summary>
    public struct Show
    {
        /// <summary>
        /// Name of the show
        /// </summary>
        public string Name;

        /// <summary>
        /// Organiser of the show
        /// </summary>
        public string Organiser;

        /// <summary>
        /// Unix time of the show
        /// </summary>
        public ulong Time;

        /// <summary>
        /// Block at which the sale ends
        /// </summary>
        public ulong EndOfSale;
    }
}