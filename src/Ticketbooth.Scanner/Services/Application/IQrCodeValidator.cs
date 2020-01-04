using System;
using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Services.Application
{
    public interface IQrCodeValidator
    {
        event EventHandler OnValidQrCode;

        Task Validate(string qrCodeData);
    }
}
