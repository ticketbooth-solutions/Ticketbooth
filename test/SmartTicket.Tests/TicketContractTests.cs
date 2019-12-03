using Moq;
using NBitcoin;
using NUnit.Framework;
using Stratis.SmartContracts;
using Stratis.SmartContracts.CLR.Serialization;
using System;
using System.Linq;
using static TicketContract;

namespace SmartTicket.Tests
{
    public class TicketContractTests
    {
        private Mock<Network> _network;
        private ISerializer _serializer;
        private Mock<ITransferResult> _transferResult;
        private Mock<IInternalTransactionExecutor> _internalTransactionExecuter;
        private Mock<IBlock> _block;
        private Mock<IMessage> _message;
        private Mock<IPersistentState> _persistentState;
        private Mock<ISmartContractState> _smartContractState;
        private Address _ownerAddress;

        private static readonly Seat[] _seats = new Seat[]
            {
                new Seat { Number = 1, Letter = 'A' }, new Seat { Number = 1, Letter = 'B' }, new Seat { Number = 1, Letter = 'C' },
                new Seat { Number = 2, Letter = 'A' }, new Seat { Number = 2, Letter = 'B' }, new Seat { Number = 2, Letter = 'C' },
                new Seat { Number = 3, Letter = 'A' }, new Seat { Number = 3, Letter = 'B' }, new Seat { Number = 3, Letter = 'C' },
                new Seat { Number = 4, Letter = 'A' }, new Seat { Number = 4, Letter = 'B' }, new Seat { Number = 4, Letter = 'C' },
                new Seat { Number = 5, Letter = 'A' }, new Seat { Number = 5, Letter = 'B' }, new Seat { Number = 5, Letter = 'C' },
            };

        private static readonly Seat _invalidSeat = new Seat { Number = 101, Letter = 'A' };

        private static Ticket[] Tickets => new Ticket[]
            {
                new Ticket { Seat = _seats[0], Price = 50 }, new Ticket { Seat = _seats[1], Price = 24 }, new Ticket { Seat = _seats[2], Price = 50 },
                new Ticket { Seat = _seats[3], Price = 60 }, new Ticket { Seat = _seats[4], Price = 45 }, new Ticket { Seat = _seats[5], Price = 52 },
                new Ticket { Seat = _seats[6], Price = 50 }, new Ticket { Seat = _seats[7], Price = 20 }, new Ticket { Seat = _seats[8], Price = 52 },
                new Ticket { Seat = _seats[9], Price = 55 }, new Ticket { Seat = _seats[10], Price = 12 }, new Ticket { Seat = _seats[11], Price = 52 },
                new Ticket { Seat = _seats[12], Price = 40 }, new Ticket { Seat = _seats[13], Price = 56 }, new Ticket { Seat = _seats[14], Price = 54 },
            };

        [SetUp]
        public void Setup()
        {
            _ownerAddress = new Address(5, 5, 4, 3, 5);
            _network = new Mock<Network>();
            _serializer = new Serializer(new ContractPrimitiveSerializer(_network.Object));
            _transferResult = new Mock<ITransferResult>();
            _internalTransactionExecuter = new Mock<IInternalTransactionExecutor>();
            _internalTransactionExecuter
                .Setup(callTo => callTo.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()))
                .Returns(_transferResult.Object);
            _block = new Mock<IBlock>();
            _message = new Mock<IMessage>();
            _persistentState = new Mock<IPersistentState>();
            _smartContractState = new Mock<ISmartContractState>();
            _smartContractState.SetupGet(callTo => callTo.Serializer).Returns(_serializer);
            _smartContractState.SetupGet(callTo => callTo.InternalTransactionExecutor).Returns(_internalTransactionExecuter.Object);
            _smartContractState.SetupGet(callTo => callTo.Block).Returns(_block.Object);
            _smartContractState.SetupGet(callTo => callTo.Message).Returns(_message.Object);
            _smartContractState.SetupGet(callTo => callTo.PersistentState).Returns(_persistentState.Object);
        }

        [Test]
        public void OnConstructed_Owner_IsSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var tickets = _serializer.Serialize(Tickets);

            // Act
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            // Assert
            _persistentState.Verify(callTo => callTo.SetAddress("Owner", _ownerAddress), Times.Once);
        }

        [Test]
        public void OnConstructed_Tickets_IsSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var tickets = _serializer.Serialize(Tickets);

            // Act
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Tickets),
                It.Is<Ticket[]>(tickets => tickets.SequenceEqual(_seats.Select(seat => new Ticket { Seat = seat, Price = 0, Address = Address.Zero })))));
        }

        [Test]
        public void OnBeginSale_NotCalledByOwner_ThrowsAssertExcption()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(new Address(3, 2, 5, 4, 2));

            // Act
            var beginSaleCall = new Action(() => ticketContract.BeginSale(tickets, 55));

            // Assert
            Assert.That(beginSaleCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnBeginSale_SaleInProgress_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(50);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var beginSaleCall = new Action(() => ticketContract.BeginSale(tickets, 55));

            // Assert
            Assert.That(beginSaleCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [TestCase((ulong)0)]
        [TestCase((ulong)50)]
        [TestCase((ulong)55)]
        public void OnBeginSale_ArgumentEndOfSale_ThrowsAssertException(ulong endOfSale)
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(55);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            var beginSaleCall = new Action(() => ticketContract.BeginSale(tickets, endOfSale));

            // Assert
            Assert.That(beginSaleCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnBeginSale_ArgumentTicketsSeatsDoesNotMatchContract_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            var ticketsParameter = (Ticket[])copyOfTickets.Clone();
            ticketsParameter[0] = new Ticket { Seat = _invalidSeat };

            // Act
            var beginSaleCall = new Action(() => ticketContract.BeginSale(_serializer.Serialize(ticketsParameter), 55));

            // Assert
            Assert.That(beginSaleCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnBeginSale_SaleCanBeOpened_ThrowsNothing()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            var beginSaleCall = new Action(() => ticketContract.BeginSale(tickets, 55));

            // Assert
            Assert.That(beginSaleCall, Throws.Nothing);
        }

        [Test]
        public void OnBeginSale_SaleCanBeOpened_TicketsAreSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            ticketContract.BeginSale(tickets, 55);

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Tickets),
                It.Is<Ticket[]>(seats => seats.SequenceEqual(copyOfTickets))));
        }

        [Test]
        public void OnBeginSale_SaleCanBeOpened_EndOfSaleIsSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            ticketContract.BeginSale(tickets, 55);

            // Assert
            _persistentState.Verify(callTo => callTo.SetUInt64(nameof(TicketContract.EndOfSale), 55));
        }

        [Test]
        public void OnEndSale_NotCalledByOwner_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(new Address(1, 3, 4, 2, 6));

            // Act
            var endSaleCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endSaleCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnEndSale_SaleNotInProgress_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var endSaleCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endSaleCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnEndSale_SaleInProgressNotFinished_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(99);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var endSaleCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endSaleCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnEndSale_SaleCanBeEnded_ThrowsNothing()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var endSaleCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endSaleCall, Throws.Nothing);
        }

        [Test]
        public void OnEndSale_SaleCanBeEnded_TicketsAreReset()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            ticketContract.EndSale();

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Tickets),
                It.Is<Ticket[]>(tickets => tickets.SequenceEqual(_seats.Select(seat => new Ticket { Seat = seat, Price = 0, Address = Address.Zero })))));
        }

        [Test]
        public void OnEndSale_SaleCanBeEnded_EndOfSaleIsSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            ticketContract.EndSale();

            // Assert
            _persistentState.Verify(callTo => callTo.SetUInt64(nameof(TicketContract.EndOfSale), 0));
        }

        [Test]
        public void OnCheckAvailability_SaleNotInProgress_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);

            // Act
            var checkAvailabilityCall = new Action(() => ticketContract.CheckAvailability(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(checkAvailabilityCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [TestCase((ulong)100)]
        [TestCase((ulong)101)]
        public void OnCheckAvailability_SaleInProgressAndFinished_ThrowsAssertException(ulong currentBlock)
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();

            _block.Setup(callTo => callTo.Number).Returns(currentBlock);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var checkAvailabilityCall = new Action(() => ticketContract.CheckAvailability(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(checkAvailabilityCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnCheckAvailability_SeatDoesNotExist_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _invalidSeat;

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(500);

            // Act
            var checkAvailabilityCall = new Action(() => ticketContract.CheckAvailability(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(checkAvailabilityCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnCheckAvailability_SeatAddressIsSet_ReturnsFalse()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            copyOfTickets[0].Address = new Address(4, 2, 2, 4, 5);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(500);

            // Act
            var availability = ticketContract.CheckAvailability(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(availability, Is.False);
        }

        [Test]
        public void OnCheckAvailability_SeatAddressIsNotSet_ReturnsTrue()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            copyOfTickets[0].Address = Address.Zero;

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(500);

            // Act
            var availability = ticketContract.CheckAvailability(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(availability, Is.True);
        }

        [Test]
        public void OnReserve_SaleNotInProgress_SendsRefundAndThrowsAssertException()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);

            // Act
            var reserveCall = new Action(() => ticketContract.Reserve(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(reserveCall, Throws.Exception.TypeOf<SmartContractAssertException>());
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, address, amount), Times.Once);
        }

        [TestCase((ulong)100)]
        [TestCase((ulong)101)]
        public void OnReserve_SaleInProgressAndFinished_SendsRefundAndThrowsAssertException(ulong currentBlock)
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(currentBlock);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var reserveCall = new Action(() => ticketContract.Reserve(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(reserveCall, Throws.Exception.TypeOf<SmartContractAssertException>());
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, address, amount), Times.Once);
        }

        [Test]
        public void OnReserve_SeatDoesNotExist_SendsRefundAndThrowsAssertException()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _invalidSeat;

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var reserveCall = new Action(() => ticketContract.Reserve(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(reserveCall, Throws.Exception.TypeOf<SmartContractAssertException>());
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, address, amount), Times.Once);
        }

        [Test]
        public void OnReserve_SeatAlreadyReserved_SendsRefundAndReturnsFalse()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            copyOfTickets[targetIndex] = targetTicket;

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var isReserved = ticketContract.Reserve(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(isReserved, Is.False);
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, address, amount), Times.Once);
        }

        [Test]
        public void OnReserve_NotEnoughFunds_SendsRefundAndReturnsFalse()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Price = amount + 1;
            copyOfTickets[targetIndex] = targetTicket;

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var isReserved = ticketContract.Reserve(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(isReserved, Is.False);
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, address, amount), Times.Once);
        }

        [Test]
        public void OnReserve_CanReserveAndTooMuchFunds_SendsRefundAndReturnsTrue()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var difference = (ulong)200;
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Price = amount - difference;
            copyOfTickets[targetIndex] = targetTicket;

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var isReserved = ticketContract.Reserve(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(isReserved, Is.True);
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, address, difference), Times.Once);
        }

        [Test]
        public void OnReserve_CanReserveAndExactFunds_DoesNotRefundAndReturnsTrue()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Price = amount;
            copyOfTickets[targetIndex] = targetTicket;

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var isReserved = ticketContract.Reserve(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(isReserved, Is.True);
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);
        }

        [Test]
        public void OnReserve_CanReserve_TicketsAreSetWithReserveAddress()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Price = amount;
            copyOfTickets[targetIndex] = targetTicket;

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            _persistentState.Invocations.Clear();

            // Act
            var isReserved = ticketContract.Reserve(_serializer.Serialize(querySeat));

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Tickets),
                It.Is<Ticket[]>(tickets => tickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter).Address == address)));
        }

        [Test]
        public void OnOwnsTicket_EmptyAddress_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);

            // Act
            var ownsTicketCall = new Action(() => ticketContract.OwnsTicket(_serializer.Serialize(querySeat), Address.Zero));

            // Assert
            Assert.That(ownsTicketCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnOwnsTicket_SeatDoesNotExist_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _invalidSeat;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);

            // Act
            var ownsTicketCall = new Action(() => ticketContract.OwnsTicket(_serializer.Serialize(querySeat), new Address(5, 3, 2, 2, 1)));

            // Assert
            Assert.That(ownsTicketCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnOwnsTicket_SeatAndTicketAddressExist_ThrowsNothing()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);

            // Act
            var ownsTicketCall = new Action(() => ticketContract.OwnsTicket(_serializer.Serialize(querySeat), new Address(5, 3, 2, 2, 1)));

            // Assert
            Assert.That(ownsTicketCall, Throws.Nothing);
        }

        [Test]
        public void OnOwnsTicket_TicketAddressIsZero_ReturnsFalse()
        {
            // Arrange
            var address = Address.Zero;
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);

            // Act
            var ownsTicket = ticketContract.OwnsTicket(_serializer.Serialize(querySeat), new Address(5, 3, 4, 5, 9));

            // Assert
            Assert.That(ownsTicket, Is.False);
        }

        [Test]
        public void OnOwnsTicket_TicketAddressDoesNotMatch_ReturnsFalse()
        {
            // Arrange
            var address = new Address(5, 3, 4, 5, 9);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);

            // Act
            var ownsTicket = ticketContract.OwnsTicket(_serializer.Serialize(querySeat), new Address(4, 5, 3, 2, 1));

            // Assert
            Assert.That(ownsTicket, Is.False);
        }

        [Test]
        public void OnOwnsTicket_TicketAddressMatches_ReturnsTrue()
        {
            // Arrange
            var address = new Address(5, 3, 4, 5, 9);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);

            // Act
            var ownsTicket = ticketContract.OwnsTicket(_serializer.Serialize(querySeat), address);

            // Assert
            Assert.That(ownsTicket, Is.True);
        }

        [Test]
        public void OnSetReleaseFee_NotCalledByOwner_ThrowsAssertExceptionFeeNotPersisted()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(new Address(5, 5, 5, 5, 99));

            // Act
            var setReleaseFeeCall = new Action(() => ticketContract.SetReleaseFee(5));

            // Assert
            Assert.That(setReleaseFeeCall, Throws.Exception.TypeOf<SmartContractAssertException>());
            _persistentState.Verify(callTo => callTo.SetUInt64(nameof(TicketContract.ReleaseFee), It.IsAny<ulong>()), Times.Never);
        }

        [Test]
        public void OnSetReleaseFee_SaleInProgress_ThrowsAssertExceptionFeeNotPersisted()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            var setReleaseFeeCall = new Action(() => ticketContract.SetReleaseFee(5));

            // Assert
            Assert.That(setReleaseFeeCall, Throws.Exception.TypeOf<SmartContractAssertException>());
            _persistentState.Verify(callTo => callTo.SetUInt64(nameof(TicketContract.ReleaseFee), It.IsAny<ulong>()), Times.Never);
        }

        [Test]
        public void OnSetReleaseFee_CanSetReleaseFee_NothingThrownReleaseFeePersisted()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(20);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            var setReleaseFeeCall = new Action(() => ticketContract.SetReleaseFee(5));

            // Assert
            Assert.That(setReleaseFeeCall, Throws.Nothing);
            _persistentState.Verify(callTo => callTo.SetUInt64(nameof(TicketContract.ReleaseFee), 5), Times.Once);
        }

        [Test]
        public void OnReleaseTicket_SaleNotInProgress_ThrowsAssertException()
        {
            // Arrange
            var address = new Address(3, 2, 4, 3, 2);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            for (var i = 0; i < copyOfTickets.Length; i++)
            {
                copyOfTickets[i].Address = address;
            }

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(101);
            _message.Setup(callTo => callTo.Sender).Returns(address);

            // Act
            var releaseTicketCall = new Action(() => ticketContract.ReleaseTicket(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(releaseTicketCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnReleaseTicket_SeatDoesNotExist_ThrowsAssertException()
        {
            // Arrange
            var address = new Address(3, 2, 4, 3, 2);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _invalidSeat;
            for (var i = 0; i < copyOfTickets.Length; i++)
            {
                copyOfTickets[i].Address = address;
            }

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _message.Setup(callTo => callTo.Sender).Returns(address);

            // Act
            var releaseTicketCall = new Action(() => ticketContract.ReleaseTicket(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(releaseTicketCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnReleaseTicket_CallerDoesNotOwnTicket_ThrowsAssertException()
        {
            // Arrange
            var address = new Address(3, 2, 4, 3, 2);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _message.Setup(callTo => callTo.Sender).Returns(new Address(5, 5, 5, 5, 5));

            // Act
            var releaseTicketCall = new Action(() => ticketContract.ReleaseTicket(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(releaseTicketCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnReleaseTicket_CanReleaseTicket_ThrowsNothing()
        {
            // Arrange
            var address = new Address(3, 2, 4, 3, 2);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _message.Setup(callTo => callTo.Sender).Returns(address);

            // Act
            var releaseTicketCall = new Action(() => ticketContract.ReleaseTicket(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(releaseTicketCall, Throws.Nothing);
        }

        [TestCase((ulong)50, (ulong)(10), (ulong)40)]
        [TestCase((ulong)50, (ulong)(30), (ulong)20)]
        [TestCase((ulong)80, (ulong)(30), (ulong)50)]
        [TestCase((ulong)80, (ulong)(0), (ulong)80)]
        public void OnReleaseTicket_CanReleaseTicket_RefundsTicketHolder(ulong ticketPrice, ulong releaseFee, ulong expectedRefund)
        {
            // Arrange
            var address = new Address(3, 2, 4, 3, 2);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            targetTicket.Price = ticketPrice;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _message.Setup(callTo => callTo.Sender).Returns(address);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.ReleaseFee))).Returns(releaseFee);

            // Act
            ticketContract.ReleaseTicket(_serializer.Serialize(querySeat));

            // Assert
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, address, expectedRefund), Times.Once);
        }

        [TestCase((ulong)50, (ulong)50)]
        [TestCase((ulong)50, (ulong)100)]
        [TestCase((ulong)0, (ulong)100)]
        [TestCase((ulong)0, (ulong)0)]
        public void OnReleaseTicket_CanReleaseTicketReleaseFeeNegatesTicketPrice_DoesNotRefundTicketHolder(ulong ticketPrice, ulong releaseFee)
        {
            // Arrange
            var address = new Address(3, 2, 4, 3, 2);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            targetTicket.Price = ticketPrice;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _message.Setup(callTo => callTo.Sender).Returns(address);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.ReleaseFee))).Returns(releaseFee);

            // Act
            ticketContract.ReleaseTicket(_serializer.Serialize(querySeat));

            // Assert
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);
        }

        [Test]
        public void OnReleaseTicket_CanReleaseTicket_TicketsAreSetWithAddressReset()
        {
            // Arrange
            var address = new Address(3, 2, 4, 3, 2);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var copyOfTickets = Tickets;
            var tickets = _serializer.Serialize(copyOfTickets);
            var ticketContract = new TicketContract(_smartContractState.Object, tickets);

            var querySeat = _seats.First();
            var targetTicket = copyOfTickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(copyOfTickets, targetTicket);
            targetTicket.Address = address;
            copyOfTickets[targetIndex] = targetTicket;

            _persistentState.Setup(callTo => callTo.GetArray<Ticket>(nameof(TicketContract.Tickets))).Returns(copyOfTickets);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _message.Setup(callTo => callTo.Sender).Returns(address);

            // Act
            ticketContract.ReleaseTicket(_serializer.Serialize(querySeat));

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Tickets),
                It.Is<Ticket[]>(tickets => tickets.First(ticket => ticket.Seat.Number == querySeat.Number && ticket.Seat.Letter == querySeat.Letter).Address == Address.Zero)));
        }
    }
}