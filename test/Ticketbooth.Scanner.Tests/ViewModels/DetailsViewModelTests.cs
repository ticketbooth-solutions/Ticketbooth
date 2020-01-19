using Easy.MessageHub;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing.Events;
using Ticketbooth.Scanner.ViewModels;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class DetailsViewModelTests
    {
        private List<TicketScanModel> _ticketScans;

        private MessageHub _eventAggregator;
        private Mock<ITicketRepository> _ticketRepository;
        private DetailsViewModel _detailsViewModel;

        [SetUp]
        public void SetUp()
        {
            _eventAggregator = new MessageHub();
            _ticketRepository = new Mock<ITicketRepository>();

            _ticketScans = new List<TicketScanModel>();
            _ticketRepository.Setup(callTo => callTo.Add(It.IsAny<TicketScanModel>()))
                .Callback(new Action<TicketScanModel>(ticketScan => _ticketScans.Add(ticketScan)));
            _ticketRepository.Setup(callTo => callTo.Find(It.IsAny<string>()))
                .Returns<string>(key => _ticketScans.FirstOrDefault(ticketScan => ticketScan.Identifier.Equals(key)));

            _detailsViewModel = new DetailsViewModel(_eventAggregator, _ticketRepository.Object);
        }

        [Test]
        public void RetrieveTicketScan_IdentifierNull_ThrowsArgumentNullException()
        {
            // Arrange
            var identifier = (string)null;

            // Act
            var retrieveTicketScanCall = new Action(() => _detailsViewModel.RetrieveTicketScan(identifier));

            // Assert
            Assert.That(retrieveTicketScanCall, Throws.Exception.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void RetrieveTicketScan_IdentifierValidCalledTwice_ThrowsInvalidOperationException()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Act
            var retrieveTicketScanCall = new Action(() => _detailsViewModel.RetrieveTicketScan(identifier));

            // Assert
            Assert.That(retrieveTicketScanCall, Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void RetrieveTicketScan_TicketScanDoesNotExist_ResultIsNotSet()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";

            // Act
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Assert
            Assert.That(_detailsViewModel.Result, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_TicketScanExists_ResultIsSet()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScan = new TicketScanModel(identifier, new SeatModel { Number = 1, Letter = 'C' });
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Assert
            Assert.That(_detailsViewModel.Result, Is.EqualTo(ticketScan));
        }

        [Test]
        public void RetrieveTicketScan_TicketScanStatusStarted_TicketScanUpdatedEventRaisesOnPropertyChanged()
        {
            // Arrange
            var eventRaised = false;
            var identifier = "09__blOoQm72n8Bf";
            var ticketScan = new TicketScanModel(identifier, new SeatModel { Number = 1, Letter = 'C' });
            _ticketScans.Add(ticketScan);
            _detailsViewModel.RetrieveTicketScan(identifier);

            _detailsViewModel.OnPropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _eventAggregator.Publish(new TicketScanUpdated(identifier));

            // Assert
            Assert.That(eventRaised, Is.True);

            _detailsViewModel.OnPropertyChanged -= (s, e) => { eventRaised = true; };
        }

        [Test]
        public void RetrieveTicketScan_DoesNotExist_MessageDetailNull()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";

            // Act
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_StatusStarted_MessageDetailNull()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScan = new TicketScanModel(identifier, new SeatModel { Number = 1, Letter = 'C' });
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_StatusCompletedDoesNotOwnTicket_MessageDetailNull()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScan = new TicketScanModel(identifier, new SeatModel { Number = 1, Letter = 'C' });
            var name = "Benjamin Rich Swift";
            ticketScan.SetScanResult(false, name);
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_StatusCompletedOwnsTicket_MessageDetailSetToName()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScan = new TicketScanModel(identifier, new SeatModel { Number = 1, Letter = 'C' });
            var name = "Benjamin Rich Swift";
            ticketScan.SetScanResult(true, name);
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.EqualTo(name));
        }

        [Test]
        public void RetrieveTicketScan_StatusFaulted_MessageDetailSet()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScan = new TicketScanModel(identifier, new SeatModel { Number = 1, Letter = 'C' });
            ticketScan.SetFaulted();
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(identifier);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Not.Null);
        }
    }
}
