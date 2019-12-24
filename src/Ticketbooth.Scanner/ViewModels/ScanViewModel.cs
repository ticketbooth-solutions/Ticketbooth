using System;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;

namespace Ticketbooth.Scanner.ViewModels
{
    public class ScanViewModel : INotifyPropertyChanged
    {
        public event EventHandler<PropertyChangedEventArgs> OnPropertyChanged;

        private readonly IQrCodeValidator _qrCodeValidator;
        private readonly IQrCodeScanner _qrCodeScanner;
        private bool _isStreaming;
        private string _errorMessage;

        public ScanViewModel(IQrCodeValidator qrCodeValidator, IQrCodeScanner qrCodeScanner)
        {
            _qrCodeValidator = qrCodeValidator;
            _qrCodeScanner = qrCodeScanner;

            _qrCodeScanner.Validation = qrCodeData => _qrCodeValidator.Validate(qrCodeData);
            _qrCodeScanner.ScanStarted = SetCameraIsOpen;
            _qrCodeScanner.CameraError = SetCameraNotOpen;
            _qrCodeScanner.CameraNotFound = SetCameraNotFound;
        }

        public bool IsStreaming
        {
            get { return _isStreaming; }
            private set
            {
                _isStreaming = value;
                OnPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStreaming)));
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set
            {
                _errorMessage = value;
                OnPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
        }

        public ValueTask StartQrScanner()
        {
            return _qrCodeScanner.Start();
        }

        public void SetCameraIsOpen()
        {
            IsStreaming = true;
        }

        public void SetCameraNotOpen()
        {
            ErrorMessage = "Allow camera access to scan tickets";
        }

        public void SetCameraNotFound()
        {
            ErrorMessage = "No cameras found";
        }
    }
}
