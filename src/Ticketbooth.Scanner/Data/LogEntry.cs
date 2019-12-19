namespace Ticketbooth.Scanner.Data
{
    public class LogEntry<T> where T : struct
    {
        public T Log { get; set; }
    }
}
