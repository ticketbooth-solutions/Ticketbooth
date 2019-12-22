using Moq;
using NUnit.Framework;
using System;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.ViewModels;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class DetailsViewModelTests
    {
        private Mock<ITicketChecker> _ticketChecker;
        private DetailsViewModel _detailsViewModel;

        [SetUp]
        public void SetUp()
        {
            _ticketChecker = new Mock<ITicketChecker>();
            _detailsViewModel = new DetailsViewModel(_ticketChecker.Object);
        }

        [Test]
        public void RetrieveTicketScan_HashNull_ThrowsArgumentNullException()
        {
            // Arrange
            var hash = (string)null;

            // Act
            var retrieveTicketScanCall = new Action(() => _detailsViewModel.RetrieveTicketScan(hash));

            // Assert
            Assert.That(retrieveTicketScanCall, Throws.Exception.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void RetrieveTicketScan_HashValidCalledTwice_ThrowsInvalidOperationException()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";
            _detailsViewModel.RetrieveTicketScan(hash);

            // Act
            var retrieveTicketScanCall = new Action(() => _detailsViewModel.RetrieveTicketScan(hash));

            // Assert
            Assert.That(retrieveTicketScanCall, Throws.Exception.TypeOf<InvalidOperationException>());
        }
    }
}
