namespace Ticketbooth.Scanner.Data.Dtos
{
    public class NodeStatus
    {
        public string ExternalAddress { get; set; }

        public string Version { get; set; }

        public string Network { get; set; }

        public NodeFeature[] FeaturesData { get; set; }

        public string State { get; set; }
    }
}
