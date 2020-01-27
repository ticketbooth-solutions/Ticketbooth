using MediatR;
using Microsoft.Extensions.Logging;
using SmartContract.Essentials.Randomness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Data;
using Ticketbooth.Scanner.Messaging.Notifications;
using Ticketbooth.Scanner.Messaging.Requests;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;
using static TicketContract;

namespace Ticketbooth.Scanner.Messaging.Handlers
{
    public class TicketScanRequestHandler : AsyncRequestHandler<TicketScanRequest>
    {
        private readonly IBlockStoreService _blockStoreService;
        private readonly ILogger<TicketScanRequestHandler> _logger;
        private readonly IMediator _mediator;
        private readonly ISmartContractService _smartContractService;
        private readonly ITicketChecker _ticketChecker;

        public TicketScanRequestHandler(IBlockStoreService blockStoreService, ILogger<TicketScanRequestHandler> logger, IMediator mediator,
            ISmartContractService smartContractService, ITicketChecker ticketChecker)
        {
            _blockStoreService = blockStoreService;
            _logger = logger;
            _mediator = mediator;
            _smartContractService = smartContractService;
            _ticketChecker = ticketChecker;
        }

        protected override async Task Handle(TicketScanRequest request, CancellationToken cancellationToken)
        {
            var stringGenerator = new UrlFriendlyStringGenerator();
            var ticketScans = new Dictionary<string, DigitalTicket>();
            foreach (var ticket in request.Tickets)
            {
                var identifier = stringGenerator.CreateUniqueString(16);
                ticketScans.Add(identifier, ticket);
                await _mediator.Publish(new TicketScanStartedNotification(identifier, ticket.Seat));
            }

            var querySeats = request.Tickets.Select(ticket => ticket.Seat);
            var groupedTicketTransactions = await RetrieveAndGroupTicketTransactionsAsync(querySeats);

            foreach (var ticketScan in ticketScans)
            {
                if (groupedTicketTransactions is null)
                {
                    // no ticket sale
                    await _mediator.Publish(new TicketScanResultNotification(ticketScan.Key, null));
                }
                else
                {
                    var ticketTransactionsGroup = groupedTicketTransactions.FirstOrDefault(group => ticketScan.Value.Seat.Equals(group.Key));
                    if (ticketTransactionsGroup is null || ticketTransactionsGroup.Count() % 2 == 0)
                    {
                        _logger.LogWarning($"Seat {ticketScan.Value.Seat.Number}{ticketScan.Value.Seat.Letter} was never purchased");
                        await _mediator.Publish(new TicketScanResultNotification(ticketScan.Key, new TicketScanResult(false, string.Empty)));
                    }
                    else
                    {
                        var ticketPurchaseTransaction = ticketTransactionsGroup.Last();
                        TicketScanResult ticketScanResult = null;
                        try
                        {
                            ticketScanResult = _ticketChecker.CheckTicket(ticketScan.Value, ticketPurchaseTransaction);
                        }
                        catch (ArgumentException e)
                        {
                            _logger.LogError(e.Message);
                        }
                        finally
                        {
                            await _mediator.Publish(new TicketScanResultNotification(ticketScan.Key, ticketScanResult));
                        }
                    }
                }
            }
        }

        private async Task<IEnumerable<IGrouping<Seat, Ticket>>> RetrieveAndGroupTicketTransactionsAsync(IEnumerable<Seat> querySeats)
        {
            var ticketSaleStart = await GetTicketSaleStartBlockHeightAsync();
            if (ticketSaleStart == default)
            {
                _logger.LogError("No ticket sale found");
                return null;
            }

            var ticketTransactionReceipts = await _smartContractService.FetchReceiptsAsync<Ticket>();
            var matchBlockHeightToTickets = ticketTransactionReceipts
                 .Select(receipt => new { receipt.BlockHash, TicketTransaction = receipt.Logs.First().Log })
                 .Where(receipt => querySeats.Contains(receipt.TicketTransaction.Seat))
                 .Select(async receipt =>
                 {
                     var block = await _blockStoreService.GetBlockDataAsync(receipt.BlockHash);
                     return new { Block = block, receipt.TicketTransaction };
                 });
            var ticketsWithBlockHeight = await Task.WhenAll(matchBlockHeightToTickets);
            return ticketsWithBlockHeight
                .Where(receipt => receipt.Block.Height > ticketSaleStart)
                .OrderBy(receipt => receipt.Block)
                .Select(receipt => receipt.TicketTransaction)
                .GroupBy(ticketTransaction => ticketTransaction.Seat);
        }

        private async Task<ulong> GetTicketSaleStartBlockHeightAsync()
        {
            var showReceipts = await _smartContractService.FetchReceiptsAsync<Show>();
            var retrieveBlockNumbers = showReceipts.Select(async receipt =>
            {
                var block = await _blockStoreService.GetBlockDataAsync(receipt.BlockHash);
                return new { Block = block };
            });
            var blockHeights = await Task.WhenAll(retrieveBlockNumbers);
            return blockHeights
                .Select(receipt => receipt.Block.Height)
                .OrderBy(blockHeight => blockHeight)
                .LastOrDefault();
        }
    }
}
