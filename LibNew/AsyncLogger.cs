using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class AsyncLogger : IDisposable
{
    private readonly string logFilePath;
    private readonly BlockingCollection<string> logQueue;
    private readonly Task logTask;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly int flushIntervalMilliseconds = 5000; // Flush every 5 seconds

    public AsyncLogger(string fileName)
    {
        logFilePath = $"logs\\{DateTime.Now:yyyy-MM-dd}.{fileName}.log"; 
        string directoryPath = Path.GetDirectoryName(logFilePath);

        // Check if the directory exists
        if (!Directory.Exists(directoryPath))
        {
            // Create the directory if it does not exist
            Directory.CreateDirectory(directoryPath);
        }
        logQueue = new BlockingCollection<string>();
        cancellationTokenSource = new CancellationTokenSource();

        // Start the log processing task
        logTask = Task.Run(() => ProcessLogQueue(cancellationTokenSource.Token));
    }

    public void Log(string message)
    {
        var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        Console.WriteLine(logMessage);
        logQueue.Add(logMessage);
    }

    private async Task ProcessLogQueue(CancellationToken cancellationToken)
    {
        using (var writer = new StreamWriter(logFilePath, true))
        {
            while (!logQueue.IsCompleted || logQueue.Count > 0)
            {
                try
                {
                    while (logQueue.TryTake(out var logMessage, flushIntervalMilliseconds))
                    {
                        await writer.WriteLineAsync(logMessage);
                    }

                    // Periodic flush even if no new logs were added
                    await writer.FlushAsync();
                }
                catch (InvalidOperationException)
                {
                    // Ignore exception if the collection is marked as complete for adding
                }
                catch (TaskCanceledException)
                {
                    // Handle cancellation, if the logger is disposed
                    break;
                }
            }

            // Final flush before exiting
            await writer.FlushAsync();
        }
    }

    public void Dispose()
    {
        // Signal cancellation
        cancellationTokenSource.Cancel();
        logQueue.CompleteAdding();

        try
        {
            // Wait for the log task to complete processing the remaining log messages
            logTask.Wait();
        }
        catch (AggregateException ex)
        {
            // Handle potential exceptions from the logging task
            foreach (var innerException in ex.InnerExceptions)
            {
                Console.WriteLine($"Logging exception: {innerException.Message}");
            }
        }
    }
}
