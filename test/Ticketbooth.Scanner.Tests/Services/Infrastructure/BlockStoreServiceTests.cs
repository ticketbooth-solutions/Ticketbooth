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
    public class BlockStoreServiceTests
    {
        private Mock<IConfiguration> _configuration;
        private Mock<ILogger<BlockStoreService>> _logger;
        private HttpTest _httpTest;
        private IBlockStoreService _blockStoreService;

        [SetUp]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<BlockStoreService>>();
            _configuration.Setup(callTo => callTo["Stratis:FullNodeApi"]).Returns("http://190.178.5.293");
            _httpTest = new HttpTest();
        }

        [TearDown]
        public void TearDown()
        {
            _httpTest.Dispose();
        }

        [Test]
        public async Task GetBlockData_200_ReturnsResponse()
        {
            // Arrange
            var receipt = new Receipt<BlockDto, object>
            {
                ReturnValue = new BlockDto { Height = 1000 }
            };
            _httpTest.RespondWithJson(receipt, status: 200);
            _blockStoreService = new BlockStoreService(_configuration.Object, _logger.Object);

            // Act
            var result = await _blockStoreService.GetBlockDataAsync("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            var expected = JsonConvert.SerializeObject(receipt.ReturnValue);
            var actual = JsonConvert.SerializeObject(result);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetBlockData_400_LogsErrorReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 400);
            _blockStoreService = new BlockStoreService(_configuration.Object, _logger.Object);

            // Act
            var result = await _blockStoreService.GetBlockDataAsync("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetBlockData_500_LogsErrorReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 500);
            _blockStoreService = new BlockStoreService(_configuration.Object, _logger.Object);

            // Act
            var result = await _blockStoreService.GetBlockDataAsync("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }
    }
}
