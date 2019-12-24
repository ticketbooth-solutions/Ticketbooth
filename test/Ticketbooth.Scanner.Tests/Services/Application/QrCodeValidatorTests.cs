using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Stratis.Sidechains.Networks;
using Stratis.SmartContracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Converters;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Tests.Fakes;

namespace Ticketbooth.Scanner.Tests.Services.Application
{
    public class QrCodeValidatorTests
    {
        private Mock<ITicketChecker> _ticketChecker;
        private FakeNavigationManager _navigationManager;
        private QrCodeValidator _qrCodeValidator;

        [SetUp]
        public void SetUp()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>() { new AddressConverter(CirrusNetwork.NetworksSelector.Mainnet.Invoke()) }
            };

            _ticketChecker = new Mock<ITicketChecker>();
            _navigationManager = new FakeNavigationManager();
            _qrCodeValidator = new QrCodeValidator(_navigationManager, _ticketChecker.Object);
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
            var count = new Random().Next(1, 10);
            var tickets = new DigitalTicket[count];
            for (int i = 0; i < count; i++)
            {
                tickets[i] = new DigitalTicket
                {
                    Seat = new TicketContract.Seat { Number = new Random().Next(1, 6), Letter = 'B' },
                    Address = new Address(50003, 39298, 382494, 323738, 432)
                };
            }

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            _ticketChecker.Verify(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
        }

        [Test]
        public async Task Validate_DataIsValidCallFailed_RedirectsBack()
        {
            // Arrange
            var tickets = new DigitalTicket[]
            {
                new DigitalTicket
                {
                    Seat = new TicketContract.Seat { Number = new Random().Next(1, 6), Letter = 'B' },
                    Address = new Address(556352392, 393654450, 1497506724, 2697943157, 1670988474)
                }
            };

            var redirects = new List<string>();
            _navigationManager.NavigationRaised += (sender, uri) => redirects.Add(uri);
            _ticketChecker
                .Setup(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(false));

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            Assert.That(redirects, Has.Count.Zero);

            _navigationManager.NavigationRaised -= (sender, uri) => redirects.Add(uri);
        }

        [Test]
        public async Task Validate_DataIsValidCallSuccess_RedirectsBack()
        {
            // Arrange
            var tickets = new DigitalTicket[]
            {
                new DigitalTicket
                {
                    Seat = new TicketContract.Seat { Number = new Random().Next(1, 6), Letter = 'B' },
                    Address = new Address(556352392, 393654450, 1497506724, 2697943157, 1670988474)
                }
            };

            var redirects = new List<string>();
            _navigationManager.NavigationRaised += (sender, uri) => redirects.Add(uri);
            _ticketChecker
                .Setup(callTo => callTo.PerformTicketCheckAsync(It.IsAny<DigitalTicket>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));

            // Act
            await _qrCodeValidator.Validate(JsonConvert.SerializeObject(tickets));

            // Assert
            Assert.Multiple(() =>
                {
                    Assert.That(redirects, Has.Count.EqualTo(1));
                    Assert.That(redirects, Has.One.EqualTo("../"));
                });

            _navigationManager.NavigationRaised -= (sender, uri) => redirects.Add(uri);
        }
    }
}
