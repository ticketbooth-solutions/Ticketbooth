using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.ViewModels;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class ScanViewModelTests
    {
        private Mock<IQrCodeValidator> _qrCodeValidator;
        private Mock<IQrCodeScanner> _qrCodeScanner;
        private ScanViewModel _scanViewModel;

        [SetUp]
        public void SetUp()
        {
            _qrCodeValidator = new Mock<IQrCodeValidator>();
            _qrCodeScanner = new Mock<IQrCodeScanner>();
        }

        [Test]
        public async Task StartQrScanner_QrCodeScanner_CallsStart()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_qrCodeValidator.Object, _qrCodeScanner.Object);

            // Act
            await _scanViewModel.StartQrScanner();

            // Assert
            _qrCodeScanner.Verify(callTo => callTo.Start(), Times.Once);
        }

        [Test]
        public void SetCameraIsOpen_IsStreaming_SetToTrue()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_qrCodeValidator.Object, _qrCodeScanner.Object);

            // Act
            _scanViewModel.SetCameraIsOpen();

            // Assert
            Assert.That(_scanViewModel.IsStreaming, Is.True);
        }

        [Test]
        public void SetCameraNotFound_ErrorMessage_SetCorrectly()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_qrCodeValidator.Object, _qrCodeScanner.Object);

            // Act
            _scanViewModel.SetCameraNotFound();

            // Assert
            Assert.That(_scanViewModel.ErrorMessage, Is.EqualTo("No cameras found"));
        }

        [Test]
        public void SetCameraNotOpen_ErrorMessage_SetCorrectly()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_qrCodeValidator.Object, _qrCodeScanner.Object);

            // Act
            _scanViewModel.SetCameraNotOpen();

            // Assert
            Assert.That(_scanViewModel.ErrorMessage, Is.EqualTo("Allow camera access to scan tickets"));
        }
    }
}
