using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public interface INodeService
    {
        Task<NodeStatus> CheckNodeStatus();
    }
}
