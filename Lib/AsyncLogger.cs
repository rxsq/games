using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

public class AsyncLogger
{
    private readonly string logFilePath;
    private readonly BlockingCollection<string> logQueue;
    private readonly Task logTask;

    public AsyncLogger(string logFilePath)
    {
        this.logFilePath = logFilePath;
        logQueue = new BlockingCollection<string>();

        // Start the log processing task
        logTask = Task.Run(ProcessLogQueue);
    }

    public void Log(string message)
    {
        var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        Console.WriteLine(logMessage);
        logQueue.Add(logMessage);
    }

    private async Task ProcessLogQueue()
    {
        using (var writer = new StreamWriter(logFilePath, true))
        {
            while (!logQueue.IsCompleted)
            {
                try
                {
                    var logMessage = logQueue.Take();
                    await writer.WriteLineAsync(logMessage);
                    await writer.FlushAsync();
                }
                catch (InvalidOperationException)
                {
                    // Ignore exception if the collection is marked as complete for adding
                }
            }
        }
    }

    public void Dispose()
    {
        logQueue.CompleteAdding();
        logTask.Wait();
    }
}
