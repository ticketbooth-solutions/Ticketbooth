using Moq;
using NUnit.Framework;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.ViewModels;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class NodeViewModelTests
    {
        private Mock<IHealthChecker> _healthChecker;
        private NodeViewModel _nodeViewModel;

        [SetUp]
        public void SetUp()
        {
            _healthChecker = new Mock<IHealthChecker>();
            _nodeViewModel = new NodeViewModel(_healthChecker.Object);
        }

        [Test]
        public void HealthChecker_PropertyChangedEvent_OnPropertyChangedRaised()
        {
            // Arrange
            var eventRaised = false;
            _nodeViewModel.OnPropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _healthChecker.Raise(callTo => callTo.OnPropertyChanged += null, null as PropertyChangedEventArgs);

            // Assert
            Assert.That(eventRaised, Is.True);

            _nodeViewModel.OnPropertyChanged -= (s, e) => { eventRaised = true; };
        }
    }
}
