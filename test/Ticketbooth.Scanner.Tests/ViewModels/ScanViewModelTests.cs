using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.Tests.Extensions;
using Ticketbooth.Scanner.ViewModels;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class ScanViewModelTests
    {
        private FakeNavigationManager _navigationManager;
        private Mock<IQrCodeScanner> _qrCodeScanner;
        private Mock<IQrCodeValidator> _qrCodeValidator;
        private ScanViewModel _scanViewModel;

        [SetUp]
        public void SetUp()
        {
            _navigationManager = new FakeNavigationManager();
            _qrCodeScanner = new Mock<IQrCodeScanner>();
            _qrCodeValidator = new Mock<IQrCodeValidator>();
        }

        [Test]
        public void QrCodeValidator_OnValidQrCode_NavigationCalled()
        {
            // Arrange
            var redirects = new List<string>();
            _navigationManager.NavigationRaised += (sender, uri) => redirects.Add(uri);
            _scanViewModel = new ScanViewModel(_navigationManager, _qrCodeScanner.Object, _qrCodeValidator.Object);

            // Act
            _qrCodeValidator.Raise(callTo => callTo.OnValidQrCode += null, null as EventArgs);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(redirects, Has.Count.EqualTo(1));
                Assert.That(redirects, Has.One.EqualTo("../"));
            });

            _navigationManager.NavigationRaised -= (sender, uri) => redirects.Add(uri);
        }

        [Test]
        public async Task StartQrScanner_QrCodeScanner_CallsStart()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_navigationManager, _qrCodeScanner.Object, _qrCodeValidator.Object);

            // Act
            await _scanViewModel.StartQrScanner();

            // Assert
            _qrCodeScanner.Verify(callTo => callTo.Start(), Times.Once);
        }

        [Test]
        public void SetCameraIsOpen_IsStreaming_SetToTrue()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_navigationManager, _qrCodeScanner.Object, _qrCodeValidator.Object);

            // Act
            _scanViewModel.SetCameraIsOpen();

            // Assert
            Assert.That(_scanViewModel.IsStreaming, Is.True);
        }

        [Test]
        public void SetCameraNotFound_ErrorMessage_SetCorrectly()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_navigationManager, _qrCodeScanner.Object, _qrCodeValidator.Object);

            // Act
            _scanViewModel.SetCameraNotFound();

            // Assert
            Assert.That(_scanViewModel.ErrorMessage, Is.EqualTo("No cameras found"));
        }

        [Test]
        public void SetCameraNotOpen_ErrorMessage_SetCorrectly()
        {
            // Arrange
            _scanViewModel = new ScanViewModel(_navigationManager, _qrCodeScanner.Object, _qrCodeValidator.Object);

            // Act
            _scanViewModel.SetCameraNotOpen();

            // Assert
            Assert.That(_scanViewModel.ErrorMessage, Is.EqualTo("Allow camera access to scan tickets"));
        }
    }
}
