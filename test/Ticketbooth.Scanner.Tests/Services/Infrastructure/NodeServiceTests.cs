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
    public class NodeServiceTests
    {
        private Mock<IConfiguration> _configuration;
        private Mock<ILogger<NodeService>> _logger;
        private HttpTest _httpTest;
        private INodeService _nodeService;

        [SetUp]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<NodeService>>();
            _configuration.Setup(callTo => callTo["Stratis:FullNodeApi"]).Returns("http://190.178.5.293");
            _httpTest = new HttpTest();
            _nodeService = new NodeService(_configuration.Object, _logger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _httpTest.Dispose();
        }

        [Test]
        public async Task CheckNodeStatus_200_ReturnsResponse()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                ExternalAddress = "192.0.0.1",
                Network = "CirrusMain",
                State = "Started"
            };

            _httpTest.RespondWithJson(nodeStatus, status: 200);

            // Act
            var result = await _nodeService.CheckNodeStatus();

            // Assert
            var expected = JsonConvert.SerializeObject(nodeStatus);
            var actual = JsonConvert.SerializeObject(result);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task CheckNodeStatus_400_LogsErrorReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 400);

            // Act
            var result = await _nodeService.CheckNodeStatus();

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task CheckNodeStatus_500_LogsWarningReturnsNull()
        {
            // Arrange
            _httpTest.RespondWith(status: 500);

            // Act
            var result = await _nodeService.CheckNodeStatus();

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
            Assert.That(result, Is.Null);
        }
    }
}