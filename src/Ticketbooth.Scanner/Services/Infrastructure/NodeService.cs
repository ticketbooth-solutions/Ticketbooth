using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public class NodeService : INodeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NodeService> _logger;

        public NodeService(IConfiguration configuration, ILogger<NodeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private IFlurlRequest BaseRequest =>
            new Url(_configuration["Stratis:FullNodeApi"])
                .AppendPathSegments("api", "node")
                .AllowHttpStatus(new HttpStatusCode[] { HttpStatusCode.OK });

        public async Task<NodeStatus> CheckNodeStatus()
        {
            try
            {
                var response = await BaseRequest
                    .AppendPathSegment("status")
                    .GetAsync();

                var nodeStatusString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<NodeStatus>(nodeStatusString);
            }
            catch (FlurlHttpException e) when (e.Call.HttpStatus == HttpStatusCode.BadRequest)
            {
                _logger.LogError(e.Message);
                return null;
            }
            catch (FlurlHttpException e)
            {
                _logger.LogWarning(e.Message);
                return null;
            }
            catch (JsonException e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }
    }
}
