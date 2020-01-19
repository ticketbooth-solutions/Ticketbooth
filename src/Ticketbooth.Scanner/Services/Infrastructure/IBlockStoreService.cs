using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public interface IBlockStoreService
    {
        Task<BlockDto> GetBlockDataAsync(string blockHash);
    }
}
