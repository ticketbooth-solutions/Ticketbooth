using Microsoft.AspNetCore.Components;
using System;

namespace Ticketbooth.Scanner.Tests.Extensions
{
    public class FakeNavigationManager : NavigationManager
    {
        public EventHandler<string> NavigationRaised { get; set; }

        protected override void EnsureInitialized()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/mycurrentlocation");
            base.EnsureInitialized();
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            NavigationRaised?.Invoke(this, uri);
        }
    }
}
