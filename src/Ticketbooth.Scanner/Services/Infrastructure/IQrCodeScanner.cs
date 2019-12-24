using System;
using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public interface IQrCodeScanner
    {
        public Func<string, Task<bool>> Validation { set; }

        public Action ScanStarted { set; }

        public Action CameraNotFound { set; }

        public Action CameraError { set; }

        ValueTask Start();
    }
}
