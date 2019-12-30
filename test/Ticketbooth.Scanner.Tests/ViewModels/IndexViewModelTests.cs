using Moq;
using NUnit.Framework;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.ViewModels;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class IndexViewModelTests
    {
        private Mock<ITicketChecker> _ticketChecker;
        private Mock<ITicketRepository> _ticketRepository;
        private IndexViewModel _indexViewModel;

        [SetUp]
        public void SetUp()
        {
            _ticketChecker = new Mock<ITicketChecker>();
            _ticketRepository = new Mock<ITicketRepository>();
            _indexViewModel = new IndexViewModel(_ticketRepository.Object, _ticketChecker.Object);
        }

        [Test]
        public void TicketChecker_OnCheckTicket_OnPropertyChangedRaised()
        {
            // Arrange
            var eventRaised = false;
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ticketCheckEventArgs = new TicketCheckRequestEventArgs(transactionHash, seat);
            _indexViewModel.OnPropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Assert
            Assert.That(eventRaised, Is.True);

            _indexViewModel.OnPropertyChanged -= (s, e) => { eventRaised = true; };
        }

        [Test]
        public void TicketChecker_OnCheckTicketResult_OnPropertyChangedRaised()
        {
            // Arrange
            var eventRaised = false;
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ticketCheckEventArgs = new TicketCheckRequestEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, true, "Benjamin Swift");
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            _indexViewModel.OnPropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(eventRaised, Is.True);

            _indexViewModel.OnPropertyChanged -= (s, e) => { eventRaised = true; };
        }
    }
}
