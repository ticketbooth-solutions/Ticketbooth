using System;
using Ticketbooth.Scanner.Eventing.Args;

namespace Ticketbooth.Scanner.Eventing
{
    public interface INotifyPropertyChanged
    {
        event EventHandler<PropertyChangedEventArgs> OnPropertyChanged;
    }
}
