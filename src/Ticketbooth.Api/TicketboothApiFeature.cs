using Stratis.Bitcoin.Builder.Feature;
using System.Threading.Tasks;

namespace Ticketbooth.Api
{
    public class TicketboothApiFeature : FullNodeFeature
    {
        public override Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
