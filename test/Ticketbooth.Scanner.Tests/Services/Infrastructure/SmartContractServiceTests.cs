using Flurl.Http.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.Tests.Extensions;

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
            var receipt = new Receipt<int>
            {
                ReturnValue = 5
            };
            _smartContractService = new SmartContractService(_configuration.Object, _logger.Object);

            _httpTest.RespondWithJson(receipt, status: 200);

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
    }
}
