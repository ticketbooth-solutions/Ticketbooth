using Microsoft.JSInterop;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Services.Infrastructure;

namespace Ticketbooth.Scanner.Tests.Services.Infrastructure
{
    public class QrCodeScannerTests
    {
        private Mock<IJSRuntime> _jsRuntime;
        private IQrCodeScanner _qrCodeScanner;

        [SetUp]
        public void SetUp()
        {
            _jsRuntime = new Mock<IJSRuntime>();
        }

        [Test]
        public async Task Start_ScannerNotStarted_CallsJsBeginScan()
        {
            // Arrange
            _qrCodeScanner = new QrCodeScanner(_jsRuntime.Object);

            // Act
            await _qrCodeScanner.Start();

            // Assert
            _jsRuntime.Verify(callTo => callTo.InvokeAsync<object>(
                "beginScan",
                It.Is<object[]>(parameters => parameters.First().GetType() == typeof(DotNetObjectReference<QrCodeScanner>))),
                Times.Once);
        }
    }
}
