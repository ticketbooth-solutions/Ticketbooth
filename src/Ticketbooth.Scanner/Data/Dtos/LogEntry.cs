namespace Ticketbooth.Scanner.Data.Dtos
{
    public class LogEntry<T> where T : struct
    {
        public T Log { get; set; }
    }
}
