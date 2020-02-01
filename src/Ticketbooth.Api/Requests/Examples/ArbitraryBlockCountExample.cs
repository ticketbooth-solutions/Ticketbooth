using Swashbuckle.AspNetCore.Examples;

namespace Ticketbooth.Api.Requests.Examples
{
    public class ArbitraryBlockCountExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return 5000;
        }
    }
}
