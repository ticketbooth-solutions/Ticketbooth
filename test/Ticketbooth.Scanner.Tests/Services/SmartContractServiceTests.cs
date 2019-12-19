using Flurl.Http.Testing;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Services;

namespace Ticketbooth.Scanner.Tests.Services
{
    public class SmartContractServiceTests
    {
        private Mock<IConfiguration> _configuration;
        private HttpTest _httpTest;
        private ISmartContractService _smartContractService;

        [SetUp]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();
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
            _smartContractService = new SmartContractService(_configuration.Object);

            _httpTest.RespondWithJson(receipt, status: 200);

            // Act
            var result = await _smartContractService.FetchReceiptAsync<int>("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            var expected = JsonConvert.SerializeObject(receipt);
            var actual = JsonConvert.SerializeObject(result);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task FetchReceipt_400_DefaultReceipt()
        {
            // Arrange
            _httpTest.RespondWith(status: 400);
            _smartContractService = new SmartContractService(_configuration.Object);

            // Act
            var result = await _smartContractService.FetchReceiptAsync<int>("hx78s8dj3uuiwejfuew98f8wef8");

            // Assert
            Assert.That(result, Is.EqualTo(default));
        }
    }
}
