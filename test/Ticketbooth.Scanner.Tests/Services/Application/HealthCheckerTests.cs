using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.Tests.Extensions;

namespace Ticketbooth.Scanner.Tests.Services.Application
{
    public class HealthCheckerTests
    {
        private const string ValidFeatureState = "Initialized";
        private const string InvalidFeatureState = "Initializing";

        private static readonly NodeFeature[] AllRequiredFeaturesReady = new NodeFeature[]
        {
            new NodeFeature { Namespace = "Stratis.Bitcoin.Base.BaseFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.SmartContracts.SmartContractFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.SmartContracts.Wallet.SmartContractWalletFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.Api.ApiFeature", State = ValidFeatureState },
        };

        private static readonly NodeFeature[] AllRequiredFeaturesNotReady = new NodeFeature[]
        {
            new NodeFeature { Namespace = "Stratis.Bitcoin.Base.BaseFeature", State = InvalidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.SmartContracts.SmartContractFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.SmartContracts.Wallet.SmartContractWalletFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.Api.ApiFeature", State = ValidFeatureState },
        };

        private static readonly NodeFeature[] SomeRequiredFeaturesReady = new NodeFeature[]
        {
            new NodeFeature { Namespace = "Stratis.Bitcoin.Base.BaseFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.BlockStore.BlockStoreFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.SmartContracts.Wallet.SmartContractWalletFeature", State = ValidFeatureState },
            new NodeFeature { Namespace = "Stratis.Bitcoin.Features.Api.ApiFeature", State = ValidFeatureState },
        };

        private Mock<INodeService> _nodeService;
        private Mock<ILogger<HealthChecker>> _logger;
        private IHealthChecker _healthChecker;

        [SetUp]
        public void SetUp()
        {
            _nodeService = new Mock<INodeService>();
            _logger = new Mock<ILogger<HealthChecker>>();
            _healthChecker = new HealthChecker(_nodeService.Object, _logger.Object);
        }

        [Test]
        public void OnConstructed_Properties_SetCorrectly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_healthChecker.IsConnected, Is.False, nameof(HealthChecker.IsConnected));
                Assert.That(_healthChecker.IsValid, Is.True, nameof(HealthChecker.IsValid));
                Assert.That(_healthChecker.IsAvailable, Is.False, nameof(HealthChecker.IsAvailable));
                Assert.That(_healthChecker.NodeVersion, Is.Null, nameof(HealthChecker.NodeVersion));
            });
        }

        [Test]
        public async Task UpdateNodeHealthAsync_NodeStatusResponse_NodeVersionIsSet()
        {
            // Arrange
            var nodeVersion = "3.0.5.0";
            var nodeStatus = new NodeStatus
            {
                State = "Starting",
                FeaturesData = SomeRequiredFeaturesReady,
                Version = nodeVersion
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.NodeVersion, Is.EqualTo(nodeVersion));
        }

        [Test]
        public async Task UpdateNodeHealthAsync_AllRequiredFeaturesInitialized_IsValidTrue()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Starting",
                FeaturesData = AllRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsValid, Is.True);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_SomeRequiredFeaturesInitialized_IsValidFalse()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Starting",
                FeaturesData = SomeRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsValid, Is.False);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_AllRequiredFeaturesNotInitialized_IsValidFalse()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Starting",
                FeaturesData = AllRequiredFeaturesNotReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsValid, Is.False);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsValidFalse_LogsWarning()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = SomeRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_NodeStatusStateStarted_IsConnectedTrue()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = SomeRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsConnected, Is.True);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_NodeStatusStateStarting_IsConnectedFalse()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Starting",
                FeaturesData = SomeRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsConnected, Is.False);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsValidTrueIsConnectedFalse_IsAvailableFalse()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Starting",
                FeaturesData = AllRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsAvailable, Is.False);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsValidFalseIsConnectedTrue_IsAvailableFalse()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = SomeRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsAvailable, Is.False);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsValidTrueIsConnectedTrue_IsAvailableTrue()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = AllRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(_healthChecker.IsAvailable, Is.True);
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsAvailableFalseToTrue_OnPropertyChangedInvoked()
        {
            var eventInvoked = false;
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = AllRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));

            _healthChecker.OnPropertyChanged += (s, e) => eventInvoked = true;

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(eventInvoked, Is.True);

            _healthChecker.OnPropertyChanged -= (s, e) => eventInvoked = true;
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsAvailableTrueToTrue_OnPropertyChangedNotInvoked()
        {
            var eventInvoked = false;
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = AllRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));
            await _healthChecker.UpdateNodeHealthAsync();

            _healthChecker.OnPropertyChanged += (s, e) => eventInvoked = true;

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(eventInvoked, Is.False);

            _healthChecker.OnPropertyChanged -= (s, e) => eventInvoked = true;
        }

        [Test]
        public async Task UpdateNodeHealthAsync_NodeStatusNull_IsConnectedFalseIsAvailableFalseNodeVersionNull()
        {
            // Arrange
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = AllRequiredFeaturesReady,
                Version = "3.0.5.0"
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));
            await _healthChecker.UpdateNodeHealthAsync();

            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(null as NodeStatus));

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_healthChecker.IsConnected, Is.False);
                Assert.That(_healthChecker.IsAvailable, Is.False);
                Assert.That(_healthChecker.NodeVersion, Is.Null);
            });
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsAvailableFalseToFalse_OnPropertyChangedNotInvoked()
        {
            var eventInvoked = false;

            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(null as NodeStatus));

            _healthChecker.OnPropertyChanged += (s, e) => eventInvoked = true;

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(eventInvoked, Is.False);

            _healthChecker.OnPropertyChanged -= (s, e) => eventInvoked = true;
        }

        [Test]
        public async Task UpdateNodeHealthAsync_IsAvailableTrueToFalse_OnPropertyChangedInvoked()
        {
            var eventInvoked = false;
            var nodeStatus = new NodeStatus
            {
                State = "Started",
                FeaturesData = AllRequiredFeaturesReady
            };
            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(nodeStatus));
            await _healthChecker.UpdateNodeHealthAsync();

            _nodeService.Setup(callTo => callTo.CheckNodeStatus()).Returns(Task.FromResult(null as NodeStatus));

            _healthChecker.OnPropertyChanged += (s, e) => eventInvoked = true;

            // Act
            await _healthChecker.UpdateNodeHealthAsync();

            // Assert
            Assert.That(eventInvoked, Is.True);

            _healthChecker.OnPropertyChanged -= (s, e) => eventInvoked = true;
        }
    }
}
