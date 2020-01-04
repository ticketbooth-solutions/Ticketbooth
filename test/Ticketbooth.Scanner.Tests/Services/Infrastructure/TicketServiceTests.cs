using Flurl.Http.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Stratis.SmartContracts;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.Tests.Extensions;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Services.Infrastructure
{
    public class TicketServiceTests
    {
        private readonly Address _validAddress = new Address(556352392, 393654450, 1497506724, 2697943157, 1670988474);

        private Mock<IConfiguration> _configuration;
        private Mock<ILogger<TicketService>> _logger;
        private HttpTest _httpTest;
        private ITicketService _ticketService;

        [SetUp]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<TicketService>>();
            _configuration.Setup(callTo => callTo["Stratis:FullNodeApi"]).Returns("http://190.178.5.293");
            _configuration.Setup(callTo => callTo["ContractAddress"]).Returns("CKBvEJbWqYqjqE5sF3aAcqHbLTr1CsF4F7");
            _configuration.Setup(callTo => callTo["Stratis:GasPrice"]).Returns("100");
            _configuration.Setup(callTo => callTo["Stratis:GasLimit"]).Returns("100000");
            _configuration.Setup(callTo => callTo["Stratis:Fee"]).Returns("0.01");
            _configuration.Setup(callTo => callTo["Stratis:Wallet"]).Returns("test_wallet");
            _configuration.Setup(callTo => callTo["Stratis:Password"]).Returns("d7hs73nfko-0kso09e");
            _configuration.Setup(callTo => callTo["Stratis:Address"]).Returns("CUtNvY1Jxpn4V4RD1tgphsUKpQdo4q5i54");
            _httpTest = new HttpTest();
        }

        [TearDown]
        public void TearDown()
        {
            _httpTest.Dispose();
        }

        [Test]
        public async Task CheckReservation_200_ReturnsResponse()
        {
            // Arrange
            var methodCallResponse = new MethodCallResponse { Success = true };
            var seat = new Seat { Number = 1, Letter = 'C' };
            var address = _validAddress;
            _ticketService = new TicketService(_logger.Object, _configuration.Object, Mock.Of<ISerializer>());

            _httpTest.RespondWithJson(methodCallResponse, status: 200);

            // Act
            var result = await _ticketService.CheckReservationAsync(seat, address);

            // Assert
            var expected = JsonConvert.SerializeObject(methodCallResponse);
            var actual = JsonConvert.SerializeObject(result);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task CheckReservation_400_LogsErrorReturnsNull()
        {
            // Arrange
            var seat = new Seat { Number = 1, Letter = 'C' };
            var address = new Address(503, 392, 93492, 394, 382);
            _ticketService = new TicketService(_logger.Object, _configuration.Object, Mock.Of<ISerializer>());

            _httpTest.RespondWith(status: 400);

            // Act
            var result = await _ticketService.CheckReservationAsync(seat, address);

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.EqualTo(null));
        }

        [Test]
        public async Task CheckReservation_500_LogsErrorReturnsNull()
        {
            // Arrange
            var seat = new Seat { Number = 1, Letter = 'C' };
            var address = new Address(503, 392, 93492, 394, 382);
            _ticketService = new TicketService(_logger.Object, _configuration.Object, Mock.Of<ISerializer>());

            _httpTest.RespondWith(status: 500);

            // Act
            var result = await _ticketService.CheckReservationAsync(seat, address);

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.EqualTo(null));
        }
    }
}
