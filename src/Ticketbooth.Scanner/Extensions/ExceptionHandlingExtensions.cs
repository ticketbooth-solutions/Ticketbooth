using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Extensions
{
    public static class ExceptionHandlingExtensions
    {
        public static Task PreserveMultipleExceptions(this Task originalTask)
        {
            var tcs = new TaskCompletionSource<object>();
            originalTask.ContinueWith(task =>
            {
                switch (task.Status)
                {
                    case TaskStatus.Canceled:
                        tcs.SetCanceled();
                        break;
                    case TaskStatus.RanToCompletion:
                        tcs.SetResult(null);
                        break;
                    case TaskStatus.Faulted:
                        tcs.SetException(originalTask.Exception);
                        break;
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }
    }
}
