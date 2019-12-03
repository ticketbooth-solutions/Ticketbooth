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

        private static Seat[] Seats => new Seat[]
            {
                new Seat { Number = 1, Letter = 'A' }, new Seat { Number = 1, Letter = 'B' }, new Seat { Number = 1, Letter = 'C' },
                new Seat { Number = 2, Letter = 'A' }, new Seat { Number = 2, Letter = 'B' }, new Seat { Number = 2, Letter = 'C' },
                new Seat { Number = 3, Letter = 'A' }, new Seat { Number = 3, Letter = 'B' }, new Seat { Number = 3, Letter = 'C' },
                new Seat { Number = 4, Letter = 'A' }, new Seat { Number = 4, Letter = 'B' }, new Seat { Number = 4, Letter = 'C' },
                new Seat { Number = 5, Letter = 'A' }, new Seat { Number = 5, Letter = 'B' }, new Seat { Number = 5, Letter = 'C' },
            };

        private static Seat[] PricedSeats => new Seat[]
            {
                new Seat { Number = 1, Letter = 'A', Price = 50 }, new Seat { Number = 1, Letter = 'B', Price = 24 }, new Seat { Number = 1, Letter = 'C', Price = 50 },
                new Seat { Number = 2, Letter = 'A', Price = 60 }, new Seat { Number = 2, Letter = 'B', Price = 45 }, new Seat { Number = 2, Letter = 'C', Price = 52 },
                new Seat { Number = 3, Letter = 'A', Price = 50 }, new Seat { Number = 3, Letter = 'B', Price = 20 }, new Seat { Number = 3, Letter = 'C', Price = 52 },
                new Seat { Number = 4, Letter = 'A', Price = 55 }, new Seat { Number = 4, Letter = 'B', Price = 12 }, new Seat { Number = 4, Letter = 'C', Price = 52 },
                new Seat { Number = 5, Letter = 'A', Price = 40 }, new Seat { Number = 5, Letter = 'B', Price = 56 }, new Seat { Number = 5, Letter = 'C', Price = 54 },
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
            var seats = _serializer.Serialize(Seats);

            // Act
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            // Assert
            _persistentState.Verify(callTo => callTo.SetAddress("Owner", _ownerAddress), Times.Once);
        }

        [Test]
        public void OnConstructed_Seats_IsSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);

            // Act
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Seats), Seats), Times.Once);
        }

        [Test]
        public void OnBeginSale_NotCalledByOwner_ThrowsAssertExcption()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var pricedSeats = _serializer.Serialize(PricedSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(Seats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(new Address(3, 2, 5, 4, 2));

            // Act
            var openCall = new Action(() => ticketContract.BeginSale(pricedSeats, 55));

            // Assert
            Assert.That(openCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnBeginSale_SaleInProgress_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var pricedSeats = _serializer.Serialize(PricedSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(Seats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(50);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var openCall = new Action(() => ticketContract.BeginSale(pricedSeats, 55));

            // Assert
            Assert.That(openCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [TestCase((ulong)0)]
        [TestCase((ulong)50)]
        [TestCase((ulong)55)]
        public void OnBeginSale_ArgumentEndOfSale_ThrowsAssertException(ulong endOfSale)
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var pricedSeats = _serializer.Serialize(PricedSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(55);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(Seats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            var openCall = new Action(() => ticketContract.BeginSale(pricedSeats, endOfSale));

            // Assert
            Assert.That(openCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnBeginSale_ArgumentSeatsDoesNotMatchContract_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var invalidPricedSeats = PricedSeats;
            invalidPricedSeats[0] = new Seat() { Number = 101, Letter = 'A' };
            var pricedSeats = _serializer.Serialize(invalidPricedSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(Seats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            var openCall = new Action(() => ticketContract.BeginSale(pricedSeats, 55));

            // Assert
            Assert.That(openCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnBeginSale_SaleCanBeOpened_ThrowsNothing()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var pricedSeats = _serializer.Serialize(PricedSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(Seats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            var openCall = new Action(() => ticketContract.BeginSale(pricedSeats, 55));

            // Assert
            Assert.That(openCall, Throws.Nothing);
        }

        [Test]
        public void OnBeginSale_SaleCanBeOpened_SeatsAreSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var pricedSeats = _serializer.Serialize(PricedSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(Seats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            ticketContract.BeginSale(pricedSeats, 55);

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Seats), It.Is<Seat[]>(seats => seats.SequenceEqual(PricedSeats))));
        }

        [Test]
        public void OnBeginSale_SaleCanBeOpened_EndOfSaleIsSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var pricedSeats = _serializer.Serialize(PricedSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(1);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(Seats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);

            // Act
            ticketContract.BeginSale(pricedSeats, 55);

            // Assert
            _persistentState.Verify(callTo => callTo.SetUInt64(nameof(TicketContract.EndOfSale), 55));
        }

        [Test]
        public void OnEndSale_NotCalledByOwner_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(new Address(1, 3, 4, 2, 6));

            // Act
            var endCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnEndSale_SaleNotInProgress_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var endCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnEndSale_SaleInProgressNotFinished_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(99);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var endCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnEndSale_SaleCanBeEnded_ThrowsNothing()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            var endCall = new Action(() => ticketContract.EndSale());

            // Assert
            Assert.That(endCall, Throws.Nothing);
        }

        [Test]
        public void OnEndSale_SaleCanBeEnded_SeatsAreSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);
            _persistentState.Setup(callTo => callTo.GetAddress("Owner")).Returns(_ownerAddress);
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            // Act
            ticketContract.EndSale();

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(TicketContract.Seats), It.Is<Seat[]>(seats => seats.SequenceEqual(Seats))));
        }

        [Test]
        public void OnEndSale_SaleCanBeEnded_EndOfSaleIsSet()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var seats = _serializer.Serialize(Seats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
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
            var copyOfSeats = Seats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
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
            var copyOfSeats = Seats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(currentBlock);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
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
            var copyOfSeats = Seats;
            var querySeat = new Seat { Number = 101, Letter = 'A' };
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
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
            var copyOfSeats = Seats;
            var copyOfPricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            copyOfPricedSeats[0].Address = new Address(4, 2, 2, 4, 5);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(copyOfPricedSeats);
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
            var copyOfSeats = Seats;
            var copyOfPricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            copyOfPricedSeats[0].Address = Address.Zero;
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(copyOfPricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(500);

            // Act
            var availability = ticketContract.CheckAvailability(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(availability, Is.True);
        }

        [Test]
        public void OnReserve_SaleNotInProgress_ThrowsAssertException()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);

            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var copyOfSeats = Seats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(1000);
            _block.Setup(callTo => callTo.Number).Returns(100);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(0);

            // Act
            var reserveCall = new Action(() => ticketContract.Reserve(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(reserveCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [TestCase((ulong)100)]
        [TestCase((ulong)101)]
        public void OnReserve_SaleInProgressAndFinished_ThrowsAssertException(ulong currentBlock)
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);

            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var copyOfSeats = Seats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(1000);
            _block.Setup(callTo => callTo.Number).Returns(currentBlock);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var reserveCall = new Action(() => ticketContract.Reserve(_serializer.Serialize(querySeat)));

            // Assert
            Assert.That(reserveCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnReserve_SeatDoesNotExist_SendsRefundAndThrowsAssertException()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;

            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var copyOfSeats = Seats;
            var querySeat = new Seat { Number = 101, Letter = 'A' };
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);
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
            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Address = address;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);
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
            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Price = amount + 1;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);
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
            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Price = amount - difference;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);
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
            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Price = amount;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            // Act
            var isReserved = ticketContract.Reserve(_serializer.Serialize(querySeat));

            // Assert
            Assert.That(isReserved, Is.True);
            _internalTransactionExecuter.Verify(callTo => callTo.Transfer(_smartContractState.Object, It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);
        }

        [Test]
        public void OnReserve_CanReserve_SeatsAreSet()
        {
            // Arrange
            var address = new Address(8, 2, 3, 3, 9);
            var amount = (ulong)1000;

            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Price = amount;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _message.Setup(callTo => callTo.Sender).Returns(address);
            _message.Setup(callTo => callTo.Value).Returns(amount);
            _block.Setup(callTo => callTo.Number).Returns(50);
            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);
            _persistentState.Setup(callTo => callTo.GetUInt64(nameof(TicketContract.EndOfSale))).Returns(100);

            _persistentState.Invocations.Clear();

            // Act
            var isReserved = ticketContract.Reserve(_serializer.Serialize(querySeat));

            // Assert
            _persistentState.Verify(callTo => callTo.SetArray(nameof(Seats), It.IsAny<Array>()), Times.Once);
        }

        [Test]
        public void OnOwnsSeat_EmptyAddress_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var copyOfSeats = Seats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);

            // Act
            var ownsSeatCall = new Action(() => ticketContract.OwnsSeat(_serializer.Serialize(querySeat), Address.Zero));

            // Assert
            Assert.That(ownsSeatCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnOwnsSeat_SeatDoesNotExist_ThrowsAssertException()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var copyOfSeats = Seats;
            var querySeat = new Seat { Number = 101, Letter = 'A' };
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);

            // Act
            var ownsSeatCall = new Action(() => ticketContract.OwnsSeat(_serializer.Serialize(querySeat), new Address(5, 3, 2, 2, 1)));

            // Assert
            Assert.That(ownsSeatCall, Throws.Exception.TypeOf<SmartContractAssertException>());
        }

        [Test]
        public void OnOwnsSeat_SeatAndAddressExist_ThrowsNothing()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);
            var copyOfSeats = Seats;
            var querySeat = copyOfSeats.First();
            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(PricedSeats);

            // Act
            var ownsSeatCall = new Action(() => ticketContract.OwnsSeat(_serializer.Serialize(querySeat), new Address(5, 3, 2, 2, 1)));

            // Assert
            Assert.That(ownsSeatCall, Throws.Nothing);
        }

        [Test]
        public void OnOwnsSeat_SeatAddressIsZero_ReturnsFalse()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var address = Address.Zero;

            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Address = address;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);

            // Act
            var ownsSeat = ticketContract.OwnsSeat(_serializer.Serialize(querySeat), new Address(5, 3, 4, 5, 9));

            // Assert
            Assert.That(ownsSeat, Is.False);
        }

        [Test]
        public void OnOwnsSeat_SeatAddressDoesNotMatch_ReturnsFalse()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var address = new Address(5, 3, 4, 5, 9);

            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Address = address;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);

            // Act
            var ownsSeat = ticketContract.OwnsSeat(_serializer.Serialize(querySeat), new Address(4, 5, 3, 2, 1));

            // Assert
            Assert.That(ownsSeat, Is.False);
        }

        [Test]
        public void OnOwnsSeat_SeatAddressMatches_ReturnsTrue()
        {
            // Arrange
            _message.Setup(callTo => callTo.Sender).Returns(_ownerAddress);

            var address = new Address(5, 3, 4, 5, 9);

            var copyOfSeats = Seats;
            var pricedSeats = PricedSeats;
            var querySeat = copyOfSeats.First();
            var targetSeat = pricedSeats.First(seat => seat.Number == querySeat.Number && seat.Letter == querySeat.Letter);
            var targetIndex = Array.IndexOf(pricedSeats, targetSeat);
            targetSeat.Address = address;
            pricedSeats[targetIndex] = targetSeat;

            var seats = _serializer.Serialize(copyOfSeats);
            var ticketContract = new TicketContract(_smartContractState.Object, seats);

            _persistentState.Setup(callTo => callTo.GetArray<Seat>(nameof(TicketContract.Seats))).Returns(pricedSeats);

            // Act
            var ownsSeat = ticketContract.OwnsSeat(_serializer.Serialize(querySeat), address);

            // Assert
            Assert.That(ownsSeat, Is.True);
        }
    }
}