using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Converters;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Requests;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Tests.Extensions;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Services.Application
{
    public class QrCodeValidatorTests
    {
        private Mock<ILogger<QrCodeValidator>> _logger;
        private Mock<IMediator> _mediator;
        private QrCodeValidator _qrCodeValidator;

        [SetUp]
        public void SetUp()
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new ByteArrayToHexConverter());
                return settings;
            };

            _logger = new Mock<ILogger<QrCodeValidator>>();
            _mediator = new Mock<IMediator>();
            _qrCodeValidator = new QrCodeValidator(_logger.Object, _mediator.Object);
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("  ")]
        [TestCase("Hello world")]
        [TestCase("{\"valid\":\"syntax\",\"cannot\":\"deserialize\"}")]
        [TestCase("[]")]
        public async Task Read_InvalidQrCodeData_DoesNotCreateTicketScanRequest(string qrCodeData)
        {
            // Arrange
            // Act
            await _qrCodeValidator.Validate(qrCodeData);

            // Assert
            _mediator.Verify(callTo => callTo.Send(It.IsAny<TicketScanRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Read_InvalidQrCodeData_OnValidQrCodeIsNotInvoked()
        {
            var eventInvoked = false;

            _qrCodeValidator.OnValidQrCode += (s, e) => { eventInvoked = true; };

            // Act
            await _qrCodeValidator.Validate("{\"valid\":\"syntax\",\"cannot\":\"deserialize\"}");

            // Assert
            Assert.That(eventInvoked, Is.False);

            _qrCodeValidator.OnValidQrCode -= (s, e) => { eventInvoked = true; };
        }

        [TestCase("Hello world")]
        [TestCase("{\"valid\":\"syntax\",\"cannot\":\"deserialize\"}")]
        public async Task Read_CannotDeserializeQrCodeData_LogsWarning(string qrCodeData)
        {
            // Arrange
            // Act
            await _qrCodeValidator.Validate(qrCodeData);

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
        }

        [Test]
        public async Task Read_ValidQrCodeData_OnValidQrCodeInvoked()
        {
            var eventInvoked = false;
            var tickets = new DigitalTicket[]
            {
                new DigitalTicket
                {
                    Seat = new Seat { Number = 2, Letter = 'D' },
                    Secret = "plaintext",
                    SecretKey = new byte[32],
                    SecretIV = new byte[16],
                    NameKey = new byte[32],
                    NameIV = new byte[16]
                }
            };
            var validQrCodeData = JsonConvert.SerializeObject(tickets);

            _qrCodeValidator.OnValidQrCode += (s, e) => { eventInvoked = true; };

            // Act
            await _qrCodeValidator.Validate(validQrCodeData);

            // Assert
            Assert.That(eventInvoked, Is.True);

            _qrCodeValidator.OnValidQrCode -= (s, e) => { eventInvoked = true; };
        }

        [Test]
        public async Task Read_ValidQrCodeData_CreatesTicketScanRequest()
        {
            // Arrange
            var itemCount = new Random().Next(1, 10);
            var tickets = new DigitalTicket[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                tickets[i] = new DigitalTicket
                {
                    Seat = new Seat { Number = i + 1, Letter = 'D' },
                    Secret = "plaintext",
                    SecretKey = new byte[32],
                    SecretIV = new byte[16],
                    NameKey = new byte[32],
                    NameIV = new byte[16]
                };
            }

            var validQrCodeData = JsonConvert.SerializeObject(tickets);

            // Act
            await _qrCodeValidator.Validate(validQrCodeData);

            // Assert
            _mediator.Verify(callTo => callTo.Send(
                It.Is<TicketScanRequest>(request => request.Tickets.Count() == tickets.Length),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
