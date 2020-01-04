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
                .Returns<string>(key => _ticketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(key)));

            _detailsViewModel = new DetailsViewModel(_eventAggregator, _ticketRepository.Object);
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

        [Test]
        public void RetrieveTicketScan_TicketScanDoesNotExist_ResultIsNotSet()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";

            // Act
            _detailsViewModel.RetrieveTicketScan(hash);

            // Assert
            Assert.That(_detailsViewModel.Result, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_TicketScanExists_ResultIsSet()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";
            var ticketScan = new TicketScanModel(hash, new SeatModel { Number = 1, Letter = 'C' });
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(hash);

            // Assert
            Assert.That(_detailsViewModel.Result, Is.EqualTo(ticketScan));
        }

        [Test]
        public void RetrieveTicketScan_TicketScanStatusStarted_TicketScanUpdatedEventRaisesOnPropertyChanged()
        {
            // Arrange
            var eventRaised = false;
            var hash = "fre8hrwhruihfjgb4iugnrj";
            var ticketScan = new TicketScanModel(hash, new SeatModel { Number = 1, Letter = 'C' });
            _ticketScans.Add(ticketScan);
            _detailsViewModel.RetrieveTicketScan(hash);

            _detailsViewModel.OnPropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _eventAggregator.Publish(new TicketScanUpdated(hash));

            // Assert
            Assert.That(eventRaised, Is.True);

            _detailsViewModel.OnPropertyChanged -= (s, e) => { eventRaised = true; };
        }

        [Test]
        public void RetrieveTicketScan_DoesNotExist_MessageDetailNull()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";

            // Act
            _detailsViewModel.RetrieveTicketScan(hash);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_StatusStarted_MessageDetailNull()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";
            var ticketScan = new TicketScanModel(hash, new SeatModel { Number = 1, Letter = 'C' });
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(hash);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_StatusCompletedDoesNotOwnTicket_MessageDetailNull()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";
            var ticketScan = new TicketScanModel(hash, new SeatModel { Number = 1, Letter = 'C' });
            var name = "Benjamin Rich Swift";
            ticketScan.SetScanResult(false, name);
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(hash);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Null);
        }

        [Test]
        public void RetrieveTicketScan_StatusCompletedOwnsTicket_MessageDetailSetToName()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";
            var ticketScan = new TicketScanModel(hash, new SeatModel { Number = 1, Letter = 'C' });
            var name = "Benjamin Rich Swift";
            ticketScan.SetScanResult(true, name);
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(hash);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.EqualTo(name));
        }

        [Test]
        public void RetrieveTicketScan_StatusFaulted_MessageDetailSet()
        {
            // Arrange
            var hash = "fre8hrwhruihfjgb4iugnrj";
            var ticketScan = new TicketScanModel(hash, new SeatModel { Number = 1, Letter = 'C' });
            ticketScan.SetFaulted();
            _ticketScans.Add(ticketScan);

            // Act
            _detailsViewModel.RetrieveTicketScan(hash);

            // Assert
            Assert.That(_detailsViewModel.MessageDetail, Is.Not.Null);
        }
    }
}
