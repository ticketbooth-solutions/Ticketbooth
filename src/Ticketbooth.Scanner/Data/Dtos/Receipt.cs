namespace Ticketbooth.Scanner.Data.Dtos
{
    public class Receipt<TValue, TLog>
    {
        public string BlockHash { get; set; }

        public TValue ReturnValue { get; set; }

        public LogDto<TLog>[] Logs { get; set; }
    }
}
