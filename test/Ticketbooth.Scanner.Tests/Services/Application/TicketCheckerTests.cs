using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SmartContract.Essentials.Ciphering;
using System;
using System.Security.Cryptography;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Data;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Tests.Extensions;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Services.Application
{
    public class TicketCheckerTests
    {
        private Mock<ICbc> _cbc;
        private Mock<ICipherFactory> _cipherFactory;
        private Mock<ILogger<TicketChecker>> _logger;
        private TicketChecker _ticketChecker;

        [SetUp]
        public void SetUp()
        {
            _cbc = new Mock<ICbc>();
            _cipherFactory = new Mock<ICipherFactory>();
            _cipherFactory.Setup(callTo => callTo.CreateCbcProvider()).Returns(_cbc.Object);
            _logger = new Mock<ILogger<TicketChecker>>();
            _ticketChecker = new TicketChecker(_cipherFactory.Object, _logger.Object);
        }

        [Test]
        public void CheckTicket_ScannedTicketNull_ThrowsArgumentNullException()
        {
            // Arrange
            var scannedTicket = null as DigitalTicket;

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'B' }
            };

            // Act
            var ticketCheckCall = new Action(() => _ticketChecker.CheckTicket(scannedTicket, actualTicket));

            // Assert
            Assert.That(ticketCheckCall, Throws.ArgumentNullException);
        }

        [Test]
        public void CheckTicket_SeatsDoNotMatch_ThrowsArgumentException()
        {
            // Arrange
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' }
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'B' }
            };

            // Act
            var ticketCheckCall = new Action(() => _ticketChecker.CheckTicket(scannedTicket, actualTicket));

            // Assert
            Assert.That(ticketCheckCall, Throws.ArgumentException);
        }

        [Test]
        public void CheckTicket_DecryptSecretThrowsCryptographicException_ReturnsResultDoesNotOwnTicket()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' }
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Throws<CryptographicException>();

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.That(result.OwnsTicket, Is.False);
        }

        [Test]
        public void CheckTicket_DecryptSecretThrowsArgumentException_LogsWarning()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' }
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Throws<ArgumentException>();

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
        }

        [Test]
        public void CheckTicket_DecryptSecretThrowsArgumentException_ReturnsNull()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' }
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Throws<ArgumentException>();

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void CheckTicket_DecryptSecretIsNull_ReturnsResultDoesNotOwnTicket()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' }
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(null as string);

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.That(result.OwnsTicket, Is.False);
        }

        [Test]
        public void CheckTicket_ProvidedSecretDoesNotMatchDecryptedSecret_ReturnsResultDoesNotOwnTicket()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = "f09aIm3-hH9379c"
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns("jwo3NM7Ux0p1kJ_");

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.That(result.OwnsTicket, Is.False);
        }

        [Test]
        public void CheckTicket_ProvidedSecretMatchesCustomerIdentifierNull_ReturnsResultOwnsTicketNameEmpty()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var plainTextSecret = "f09aIm3-hH9379c";
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = plainTextSecret
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(plainTextSecret);

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.OwnsTicket, Is.True, nameof(TicketScanResult.OwnsTicket));
                Assert.That(result.Name, Is.Empty, nameof(TicketScanResult.Name));
            });
        }

        [Test]
        public void CheckTicket_ProvidedSecretMatchesDecryptCustomerIdentifierThrowsCryptographicException_ReturnsResultOwnsTicketNameEmpty()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var customerIdentifier = new byte[16] { 33, 93, 23, 252, 24, 38, 43, 94, 224, 10, 12, 232, 28, 211, 64, 99 };
            var plainTextSecret = "f09aIm3-hH9379c";
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = plainTextSecret
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret,
                CustomerIdentifier = customerIdentifier
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(plainTextSecret);
            _cbc.Setup(callTo => callTo.Decrypt(customerIdentifier, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Throws<CryptographicException>();

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.OwnsTicket, Is.True, nameof(TicketScanResult.OwnsTicket));
                Assert.That(result.Name, Is.Empty, nameof(TicketScanResult.Name));
            });
        }

        [Test]
        public void CheckTicket_ProvidedSecretMatchesDecryptCustomerIdentifierThrowsArgumentException_LogsWarning()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var customerIdentifier = new byte[16] { 33, 93, 23, 252, 24, 38, 43, 94, 224, 10, 12, 232, 28, 211, 64, 99 };
            var plainTextSecret = "f09aIm3-hH9379c";
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = plainTextSecret
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret,
                CustomerIdentifier = customerIdentifier
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(plainTextSecret);
            _cbc.Setup(callTo => callTo.Decrypt(customerIdentifier, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Throws<ArgumentException>();

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
        }

        [Test]
        public void CheckTicket_ProvidedSecretMatchesDecryptCustomerIdentifierThrowsArgumentException_ReturnsNull()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var customerIdentifier = new byte[16] { 33, 93, 23, 252, 24, 38, 43, 94, 224, 10, 12, 232, 28, 211, 64, 99 };
            var plainTextSecret = "f09aIm3-hH9379c";
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = plainTextSecret
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret,
                CustomerIdentifier = customerIdentifier
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(plainTextSecret);
            _cbc.Setup(callTo => callTo.Decrypt(customerIdentifier, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Throws<ArgumentException>();

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void CheckTicket_ProvidedSecretMatchesCustomerIdentifierDecrypted_ReturnsResultOwnsTicketNameMatchesDecryptedValue()
        {
            // Arrange
            var secret = new byte[16] { 203, 92, 1, 93, 84, 38, 27, 94, 190, 10, 199, 232, 28, 2, 34, 83 };
            var customerIdentifier = new byte[16] { 33, 93, 23, 252, 24, 38, 43, 94, 224, 10, 12, 232, 28, 211, 64, 99 };
            var plainTextSecret = "f09aIm3-hH9379c";
            var name = "Benjamin Swift";
            var scannedTicket = new DigitalTicket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = plainTextSecret
            };

            var actualTicket = new Ticket
            {
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = secret,
                CustomerIdentifier = customerIdentifier
            };

            _cbc.Setup(callTo => callTo.Decrypt(secret, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(plainTextSecret);
            _cbc.Setup(callTo => callTo.Decrypt(customerIdentifier, It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(name);

            // Act
            var result = _ticketChecker.CheckTicket(scannedTicket, actualTicket);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.OwnsTicket, Is.True, nameof(TicketScanResult.OwnsTicket));
                Assert.That(result.Name, Is.EqualTo(name), nameof(TicketScanResult.Name));
            });
        }
    }
}
