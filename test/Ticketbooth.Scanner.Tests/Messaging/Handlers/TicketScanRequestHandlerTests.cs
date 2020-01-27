using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Stratis.SmartContracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Data;
using Ticketbooth.Scanner.Messaging.Handlers;
using Ticketbooth.Scanner.Messaging.Notifications;
using Ticketbooth.Scanner.Messaging.Requests;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.Tests.Extensions;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Messaging.Handlers
{
    public class TicketScanRequestHandlerTests
    {
        private static readonly Receipt<object, Show>[] _showReceipts = new Receipt<object, Show>[]
        {
            new Receipt<object, Show>
            {
                BlockHash = "ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9",
                Logs = new LogDto<Show>[]
                {
                    new LogDto<Show>
                    {
                        Log = new Show
                        {
                            Name = "Greatest Hits Tour",
                            Organiser = "Rick Astley",
                            Time = 1587063600,
                            EndOfSale = 12000
                        }
                    }
                }
            }
        };

        private Mock<IBlockStoreService> _blockStoreService;
        private Mock<ILogger<TicketScanRequestHandler>> _logger;
        private Mock<IMediator> _mediator;
        private Mock<ISmartContractService> _smartContractService;
        private Mock<ITicketChecker> _ticketChecker;
        private IRequestHandler<TicketScanRequest> _ticketScanRequestHandler;

        [SetUp]
        public void SetUp()
        {
            _blockStoreService = new Mock<IBlockStoreService>();
            _logger = new Mock<ILogger<TicketScanRequestHandler>>();
            _mediator = new Mock<IMediator>();
            _smartContractService = new Mock<ISmartContractService>();
            _ticketChecker = new Mock<ITicketChecker>();
            _ticketScanRequestHandler = new TicketScanRequestHandler(_blockStoreService.Object, _logger.Object, _mediator.Object,
                _smartContractService.Object, _ticketChecker.Object);
        }

        [Test]
        public async Task Handle_TicketsProvided_PublishesTicketScanStartedNotifications()
        {
            // Arrange
            var ticketCount = new Random().Next(0, 5);
            var tickets = new DigitalTicket[ticketCount];
            for (int i = 0; i < ticketCount; i++)
            {
                tickets[i] = new DigitalTicket { Seat = new Seat { Number = i + 1, Letter = 'A' } };
            }

            var ticketScanRequest = new TicketScanRequest(tickets);

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.IsAny<TicketScanStartedNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(ticketCount));
        }

        [Test]
        public async Task Handle_NoMatchingTicketTransactions_PublishesInvalidTicketScanResultNotification()
        {
            // Arrange
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransactionReceipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = new Ticket
                            {
                                Address = new Address(5, 5, 4, 3, 5),
                                Price = 2490000000,
                                Seat = new Seat { Number = 2, Letter = 'A' },
                                Secret = new byte[16],
                                CustomerIdentifier = new byte[16]
                            }
                        }
                    }
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.Is<TicketScanResultNotification>(notification => !notification.Result.OwnsTicket),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_EvenMatchingTicketTransactions_PublishesInvalidTicketScanResultNotification()
        {
            // Arrange
            var ticketCount = new Random().Next(1, 3) * 2;
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransactionReceipts = new Receipt<object, Ticket>[ticketCount];
            for (int i = 0; i < ticketCount; i++)
            {
                ticketTransactionReceipts[i] = new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = new Ticket
                            {
                                Address = new Address(5, 5, 4, 3, 5),
                                Price = 2490000000,
                                Seat = new Seat { Number = 1, Letter = 'A' },
                                Secret = i % 2 == 0 ? new byte[16] : null,
                                CustomerIdentifier = i % 2 == 0 ? new byte[16] : null
                            }
                        }
                    }
                };
            }

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.Is<TicketScanResultNotification>(notification => !notification.Result.OwnsTicket),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_NoMatchingTicketTransactions_LogsWarning()
        {
            // Arrange
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransactionReceipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = new Ticket
                            {
                                Address = new Address(5, 5, 4, 3, 5),
                                Price = 2490000000,
                                Seat = new Seat { Number = 2, Letter = 'A' },
                                Secret = new byte[16],
                                CustomerIdentifier = new byte[16]
                            }
                        }
                    }
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
        }

        [Test]
        public async Task Handle_EvenMatchingTicketTransactions_LogsWarning()
        {
            // Arrange
            var ticketCount = new Random().Next(1, 3) * 2;
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransactionReceipts = new Receipt<object, Ticket>[ticketCount];
            for (int i = 0; i < ticketCount; i++)
            {
                ticketTransactionReceipts[i] = new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = new Ticket
                            {
                                Address = new Address(5, 5, 4, 3, 5),
                                Price = 2490000000,
                                Seat = new Seat { Number = 1, Letter = 'A' },
                                Secret = i % 2 == 0 ? new byte[16] : null,
                                CustomerIdentifier = i % 2 == 0 ? new byte[16] : null
                            }
                        }
                    }
                };
            }

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Handle_OddMatchingTicketTransactions_PublishesCorrectTicketScanResultNotification(bool ownsTicket)
        {
            // Arrange
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransaction = new Ticket
            {
                Address = new Address(5, 5, 4, 3, 5),
                Price = 2490000000,
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = new byte[16],
                CustomerIdentifier = new byte[16]
            };

            var ticketTransactionReceipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = ticketTransaction
                        }
                    }
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));
            _ticketChecker.Setup(callTo => callTo.CheckTicket(ticket, ticketTransaction)).Returns(new TicketScanResult(ownsTicket, default));

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.Is<TicketScanResultNotification>(notification => notification.Result.OwnsTicket == ownsTicket),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_OddMatchingTicketTransactionsAndOwnsTicket_PublishesCorrectlyNamedTicketScanResultNotification()
        {
            // Arrange
            var name = "Benjamin Swift";
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransaction = new Ticket
            {
                Address = new Address(5, 5, 4, 3, 5),
                Price = 2490000000,
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = new byte[16],
                CustomerIdentifier = new byte[16]
            };

            var ticketTransactionReceipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = ticketTransaction
                        }
                    }
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));
            _ticketChecker.Setup(callTo => callTo.CheckTicket(ticket, ticketTransaction)).Returns(new TicketScanResult(true, name));

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.Is<TicketScanResultNotification>(notification => notification.Result.Name.Equals(name)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_OddMatchingTicketTransactionsButNewShowSince_DoesNotCountOldTransactionsAndLogsWarning()
        {
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransaction = new Ticket
            {
                Address = new Address(5, 5, 4, 3, 5),
                Price = 2490000000,
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = new byte[16],
                CustomerIdentifier = new byte[16]
            };

            var ticketTransactionReceipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = ticketTransaction
                        }
                    }
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 3 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _logger.VerifyLog(LogLevel.Warning);
        }

        [Test]
        public async Task Handle_CheckTicketThrowsArgumentException_LogsError()
        {
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransaction = new Ticket
            {
                Address = new Address(5, 5, 4, 3, 5),
                Price = 2490000000,
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = new byte[16],
                CustomerIdentifier = new byte[16]
            };

            var ticketTransactionReceipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = ticketTransaction
                        }
                    }
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));
            _ticketChecker.Setup(callTo => callTo.CheckTicket(ticket, ticketTransaction)).Throws<ArgumentException>();

            // Act
            await _ticketScanRequestHandler.Handle(ticketScanRequest, default);

            // Assert
            _logger.VerifyLog(LogLevel.Error);
        }

        [Test]
        public async Task Handle_CheckTicketThrowsException_PublishesTicketScanResultNotificationNullResult()
        {
            var ticket = new DigitalTicket() { Seat = new Seat { Number = 1, Letter = 'A' } };
            var ticketScanRequest = new TicketScanRequest(ticket);

            var ticketTransaction = new Ticket
            {
                Address = new Address(5, 5, 4, 3, 5),
                Price = 2490000000,
                Seat = new Seat { Number = 1, Letter = 'A' },
                Secret = new byte[16],
                CustomerIdentifier = new byte[16]
            };

            var ticketTransactionReceipts = new Receipt<object, Ticket>[]
            {
                new Receipt<object, Ticket>
                {
                    BlockHash = "vZxae80111cab8c7e8f932289fdda9a2",
                    Logs = new LogDto<Ticket>[]
                    {
                        new LogDto<Ticket>
                        {
                            Log = ticketTransaction
                        }
                    }
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Ticket>()).Returns(Task.FromResult(ticketTransactionReceipts));
            _smartContractService.Setup(callTo => callTo.FetchReceiptsAsync<Show>()).Returns(Task.FromResult(_showReceipts));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("ks9I72n9Hjj9azM2l9D3Palq2jfBjkb9")).Returns(Task.FromResult(new BlockDto { Height = 1 }));
            _blockStoreService.Setup(callTo => callTo.GetBlockDataAsync("vZxae80111cab8c7e8f932289fdda9a2")).Returns(Task.FromResult(new BlockDto { Height = 2 }));
            _ticketChecker.Setup(callTo => callTo.CheckTicket(ticket, ticketTransaction)).Throws<Exception>();

            // Act
            try
            {
                await _ticketScanRequestHandler.Handle(ticketScanRequest, default);
            }
            catch (Exception)
            {
            }

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.Is<TicketScanResultNotification>(notification => notification.Result == null),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
