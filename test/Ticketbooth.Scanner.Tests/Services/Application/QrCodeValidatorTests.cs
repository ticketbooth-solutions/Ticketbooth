using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Stratis.Sidechains.Networks;
using Stratis.SmartContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Converters;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Notifications;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Tests.Extensions;

namespace Ticketbooth.Scanner.Tests.Services.Application
{
    public class QrCodeValidatorTests
    {
        private static readonly Random Random = new Random();

        private Mock<ILogger<QrCodeValidator>> _logger;
        private Mock<IMediator> _mediator;
        private Mock<ITicketChecker> _ticketChecker;
        private QrCodeValidator _qrCodeValidator;

        [SetUp]
        public void SetUp()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>() { new AddressConverter(CirrusNetwork.NetworksSelector.Mainnet.Invoke()) }
            };

            _logger = new Mock<ILogger<QrCodeValidator>>();
            _mediator = new Mock<IMediator>();
            _ticketChecker = new Mock<ITicketChecker>();
            _qrCodeValidator = new QrCodeValidator(_logger.Object, _mediator.Object, _ticketChecker.Object);
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task Validate_NoDataProvided_DoesNotCheckTickets(string qrCodeData)
        {
            // Arrange
            // Act
            await _qrCodeValidator.Validate(qrCodeData);

            // Assert
            _ticketChecker.Verify(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Validate_IncorrectDataProvided_DoesNotCheckTickets()
        {
            // Arrange
            var qrCodeData = "89fh4fueiuwfhiufhuwfhiu32";

            // Act
            await _qrCodeValidator.Validate(qrCodeData);

            // Assert
            _ticketChecker.Verify(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Validate_NoTicketsProvided_DoesNotCheckTickets()
        {
            // Arrange
            var tickets = new DigitalTicket[0];

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            _ticketChecker.Verify(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Validate_DataIsValid_PerformsTicketChecks()
        {
            // Arrange
            var count = Random.Next(1, 10);
            var tickets = new DigitalTicket[count];
            for (int i = 0; i < count; i++)
            {
                tickets[i] = new DigitalTicket
                {
                    Seat = new TicketContract.Seat { Number = Random.Next(1, 6), Letter = 'B' },
                    Address = new Address(50003, 39298, 382494, 323738, 432)
                };
            }

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            _ticketChecker.Verify(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
        }

        [Test]
        public async Task Validate_DataIsValidNoTicketCheckTransactions_NoEventsOrNotifications()
        {
            // Arrange
            var eventInvoked = false;
            var tickets = new DigitalTicket[]
            {
                new DigitalTicket
                {
                    Seat = new TicketContract.Seat { Number = Random.Next(1, 6), Letter = 'B' },
                    Address = new Address(556352392, 393654450, 1497506724, 2697943157, 1670988474)
                }
            };

            _ticketChecker
                .Setup(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(null as string));

            _qrCodeValidator.OnValidQrCode += (s, e) => eventInvoked = true;

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            Assert.That(eventInvoked, Is.False);
            _mediator.Verify(callTo => callTo.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);

            _qrCodeValidator.OnValidQrCode -= (s, e) => eventInvoked = true;
        }

        [Test]
        public async Task Validate_DataIsValidSomeXTicketCheckTransactions_EventInvokedAndSomeNotifications()
        {
            // Arrange
            var eventInvoked = false;
            var count = Random.Next(4, 12);
            var tickets = new DigitalTicket[count];
            for (int i = 0; i < count; i++)
            {
                tickets[i] = new DigitalTicket
                {
                    Seat = new TicketContract.Seat { Number = i + 1, Letter = 'B' },
                    Address = new Address(556352392, 393654450, 1497506724, 2697943157, 1670988474)
                };
            }

            var testData = tickets.ToDictionary(ticket => Generate.String(8));
            var failCount = 0;
            for (int x = 0; x < testData.Count; x++)
            {
                var entry = testData.ElementAt(x);
                var fail = x % 4 == 0;
                if (fail)
                {
                    failCount++;
                }

                _ticketChecker
                    .Setup(callTo => callTo.PerformTicketCheckAsync(It.Is<DigitalTicket>(
                        ticket => JsonConvert.SerializeObject(ticket).Equals(JsonConvert.SerializeObject(entry.Value))), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(fail ? null as string : entry.Key));
            }

            _qrCodeValidator.OnValidQrCode += (s, e) => eventInvoked = true;

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            Assert.That(eventInvoked, Is.True);
            _mediator.Verify(
                callTo => callTo.Publish(It.Is<TicketScanStartedNotification>(
                    notification => testData.ContainsKey(notification.TransactionHash)
                    && testData[notification.TransactionHash].Seat.Equals(notification.Seat)), It.IsAny<CancellationToken>()),
                Times.Exactly(count - failCount));

            _qrCodeValidator.OnValidQrCode -= (s, e) => eventInvoked = true;
        }

        [Test]
        public async Task Validate_DataIsValidAllXTicketCheckTransactions_EventInvokedAndXNotifications()
        {
            // Arrange
            var eventInvoked = false;
            var count = Random.Next(1, 10);
            var tickets = new DigitalTicket[count];
            for (int i = 0; i < count; i++)
            {
                tickets[i] = new DigitalTicket
                {
                    Seat = new TicketContract.Seat { Number = i + 1, Letter = 'B' },
                    Address = new Address(556352392, 393654450, 1497506724, 2697943157, 1670988474)
                };
            }

            var testData = tickets.ToDictionary(ticket => Generate.String(8));
            foreach (var entry in testData)
            {
                _ticketChecker
                    .Setup(callTo => callTo.PerformTicketCheckAsync(It.Is<DigitalTicket>(
                        ticket => JsonConvert.SerializeObject(ticket).Equals(JsonConvert.SerializeObject(entry.Value))), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(entry.Key));
            }

            _qrCodeValidator.OnValidQrCode += (s, e) => eventInvoked = true;

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            Assert.That(eventInvoked, Is.True);
            _mediator.Verify(
                callTo => callTo.Publish(It.Is<TicketScanStartedNotification>(
                    notification => testData.ContainsKey(notification.TransactionHash)
                    && testData[notification.TransactionHash].Seat.Equals(notification.Seat)), It.IsAny<CancellationToken>()),
                Times.Exactly(count));

            _qrCodeValidator.OnValidQrCode -= (s, e) => eventInvoked = true;
        }
    }
}
