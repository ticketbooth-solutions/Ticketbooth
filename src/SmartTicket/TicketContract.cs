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

    public Seat[] Seats
    {
        get
        {
            return PersistentState.GetArray<Seat>(nameof(Seats));
        }
        private set
        {
            PersistentState.SetArray(nameof(Seats), value);
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

    public TicketContract(ISmartContractState smartContractState, byte[] seatsBytes) : base(smartContractState)
    {
        PersistentState.SetAddress(nameof(Owner), Message.Sender);
        Seats = ResetSeats(Serializer.ToArray<Seat>(seatsBytes));
    }

    public void BeginSale(byte[] seatsBytes, ulong endOfSale)
    {
        Assert(Message.Sender == Owner, "Only contract owner can begin a sale");
        Assert(EndOfSale == default(ulong), "Sale currently in progress");
        Assert(Block.Number < endOfSale, "Sale must finish in the future");

        var seats = Serializer.ToArray<Seat>(seatsBytes);
        var copyOfSeats = Seats;

        Assert(copyOfSeats.Length == seats.Length, "Seat elements must be equal");
        for (int i = 0; i < copyOfSeats.Length; i++)
        {
            Seat seat = default(Seat);
            for (int y = 0; y < seats.Length; y++)
            {
                if (copyOfSeats[i].Number == seats[y].Number && copyOfSeats[i].Letter == seats[y].Letter)
                {
                    seat = seats[y];
                }
            }
            Assert(!IsDefaultSeat(seat), "Invalid seat provided");
            copyOfSeats[i].Price = seats[i].Price;
        }

        Seats = copyOfSeats;
        EndOfSale = endOfSale;
    }

    public void EndSale()
    {
        Assert(Message.Sender == Owner, "Only contract owner can end sale");
        Assert(EndOfSale != default(ulong), "Sale not currently in progress");
        Assert(Block.Number >= EndOfSale, "Sale contract not fulfilled");

        Seats = ResetSeats(Seats);
        EndOfSale = default(ulong);
    }

    public bool CheckAvailability(byte[] seatIdentifierBytes)
    {
        Assert(EndOfSale != 0 && Block.Number < EndOfSale, "Sale not in progress");

        var selectedSeat = SelectSeat(seatIdentifierBytes);

        Assert(!IsDefaultSeat(selectedSeat), "Seat not found");

        return IsAvailable(selectedSeat);
    }

    public bool Reserve(byte[] seatIdentifierBytes)
    {
        Assert(EndOfSale != 0 && Block.Number < EndOfSale, "Sale not in progress");

        var selectedSeat = SelectSeat(seatIdentifierBytes);

        // seat not found
        if (IsDefaultSeat(selectedSeat))
        {
            Refund(Message.Value);
            Assert(false, "Seat not found");
        }

        // already reserved
        if (!IsAvailable(selectedSeat))
        {
            Refund(Message.Value);
            return false;
        }

        // not enough funds
        if (Message.Value < selectedSeat.Price)
        {
            Refund(Message.Value);
            return false;
        }

        if (Message.Value > selectedSeat.Price)
        {
            Refund(Message.Value - selectedSeat.Price);
        }

        selectedSeat.Address = Message.Sender;
        return true;
    }

    public bool OwnsSeat(byte[] seatIdentifierBytes, Address address)
    {
        Assert(address != Address.Zero, "Invalid address");

        var selectedSeat = SelectSeat(seatIdentifierBytes);

        Assert(!IsDefaultSeat(selectedSeat), "Seat not found");

        return selectedSeat.Address == address;
    }

    public Seat[] GetSeats() => Seats;

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

    private Seat SelectSeat(byte[] seatIdentifierBytes)
    {
        var seatIdentifier = Serializer.ToStruct<Seat>(seatIdentifierBytes);
        foreach (var seat in Seats)
        {
            if (seat.Number == seatIdentifier.Number && seat.Letter == seatIdentifier.Letter)
            {
                return seat;
            }
        }

        return default(Seat);
    }

    private bool IsAvailable(Seat seat)
    {
        return seat.Address == Address.Zero;
    }

    private bool IsDefaultSeat(Seat seat)
    {
        return seat.Number == default(int) || seat.Letter == default(char);
    }

    private Seat[] ResetSeats(Seat[] seats)
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

        public ulong Price;

        public Address Address;
    }
}