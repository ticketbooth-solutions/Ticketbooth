using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Extensions;

namespace Ticketbooth.Scanner.Messaging
{
    public class ParallelMediator : Mediator
    {
        private readonly ILogger<ParallelMediator> _logger;

        public ParallelMediator(ILogger<ParallelMediator> logger, ServiceFactory serviceFactory) : base(serviceFactory)
        {
            _logger = logger;
        }

        protected override async Task PublishCore(IEnumerable<Func<INotification, CancellationToken, Task>> allHandlers, INotification notification, CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(allHandlers.Select(handler => handler(notification, cancellationToken))).PreserveMultipleExceptions();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    _logger.LogCritical(e.Message);
                }
            }
        }
    }
}
