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
    public class SmartContractServiceTests
    {
        private Mock<IConfiguration> _configuration;
        private Mock<ILogger<SmartContractService>> _logger;
        private HttpTest _httpTest;
        private ISmartContractService _smartContractService;

        [SetUp]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<SmartContractService>>();
            _configuration.Setup(callTo => callTo["Stratis:FullNodeApi"]).Returns("http://190.178.5.293");
            _configuration.Setup(callTo => callTo["ContractAddress"]).Returns("CGsBaREiqSF6VbmTSnGTKknE1RRbNpjXco");
            _httpTest = new HttpTest();
        }

        [TearDown]
        public void TearDown()
        {
            _httpTest.Dispose();
        }

        [Test]
        public async Task FetchReceipt_200_ReturnsResponse()
        {
            // Arrange
            var receipt = new Receipt<int, object>
            {
                ReturnValue = 5
            };
            _httpTest.RespondWithJson(receipt, status: 200);
            _smartContractService = new SmartContractService(_configuration.Object, _logger.Object);

            // Act
            var result = await _smartContractService.FetchReceiptAsync<int>("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            var expected = JsonConvert.SerializeObject(receipt);
            var actual = JsonConvert.SerializeObject(result);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task FetchReceipt_400_ReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 400);
            _smartContractService = new SmartContractService(_configuration.Object, _logger.Object);

            // Act
            var result = await _smartContractService.FetchReceiptAsync<int>("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task FetchReceipt_500_LogsErrorReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 500);
            _smartContractService = new SmartContractService(_configuration.Object, _logger.Object);

            // Act
            var result = await _smartContractService.FetchReceiptAsync<int>("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task FetchReceipts_200_ReturnsResponse()
        {
            // Arrange
            var receipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = new Ticket
                            {
                                Address = Address.Zero,
                                Seat = new Seat { Number = 2, Letter = 'D' },
                                Price = 249500000,
                                Secret = new byte[16],
                                CustomerIdentifier = new byte[32]
                            }
                        }
                    }
                }
            };
            _httpTest.RespondWithJson(receipts, status: 200);
            _smartContractService = new SmartContractService(_configuration.Object, _logger.Object);

            // Act
            var result = await _smartContractService.FetchReceiptsAsync<Ticket>();

            // Assert
            var expected = JsonConvert.SerializeObject(receipts);
            var actual = JsonConvert.SerializeObject(result);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task FetchReceipts_400_LogsErrorReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 400);
            _smartContractService = new SmartContractService(_configuration.Object, _logger.Object);

            // Act
            var result = await _smartContractService.FetchReceiptsAsync<Ticket>();

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task FetchReceipts_500_LogsErrorReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 500);
            _smartContractService = new SmartContractService(_configuration.Object, _logger.Object);

            // Act
            var result = await _smartContractService.FetchReceiptsAsync<Ticket>();

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }
    }
}
