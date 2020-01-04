using Easy.MessageHub;
using Moq;
using NUnit.Framework;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Eventing.Events;
using Ticketbooth.Scanner.ViewModels;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class IndexViewModelTests
    {
        private IMessageHub _eventAggregator;
        private Mock<ITicketRepository> _ticketRepository;
        private IndexViewModel _indexViewModel;

        [SetUp]
        public void SetUp()
        {
            _eventAggregator = new MessageHub();
            _ticketRepository = new Mock<ITicketRepository>();
            _indexViewModel = new IndexViewModel(_eventAggregator, _ticketRepository.Object);
        }

        [Test]
        public void TicketChecker_TicketScanAddedEvent_OnPropertyChangedRaised()
        {
            // Arrange
            var eventRaised = false;
            _indexViewModel.OnPropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _eventAggregator.Publish(new TicketScanAdded("dsfjs89wj"));

            // Assert
            Assert.That(eventRaised, Is.True);

            _indexViewModel.OnPropertyChanged -= (s, e) => { eventRaised = true; };
        }

        [Test]
        public void TicketChecker_TicketScanUpdatedEvent_OnPropertyChangedRaised()
        {
            // Arrange
            var eventRaised = false;
            _indexViewModel.OnPropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _eventAggregator.Publish(new TicketScanUpdated("dsfjs89wj"));

            // Assert
            Assert.That(eventRaised, Is.True);

            _indexViewModel.OnPropertyChanged -= (s, e) => { eventRaised = true; };
        }
    }
}
