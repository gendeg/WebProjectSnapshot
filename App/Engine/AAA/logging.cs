using AppSettings;

namespace Logging;

/*public class Log
{
    readonly static string logFilePath = Settings.Get().rootPath + Settings.LookupString("logFile");
    readonly static StreamWriter logFileWriter = new(logFilePath, append: true);
    public readonly static ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            //Add console output
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
                options.UseUtcTimestamp = true;
            });

            //Add a custom log provider to write logs to text files
            builder.AddProvider(new FileLogProvider(logFileWriter));
        });
    public static ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

    public static ILogger<Program> Get()
    {
        return logger;
    }
}*/


public class FileLogProvider : ILoggerProvider
{
    private readonly StreamWriter _logFileWriter;

    public FileLogProvider(StreamWriter logFileWriter)
    {
        _logFileWriter = logFileWriter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _logFileWriter);
    }

    public void Dispose()
    {
        _logFileWriter.Dispose();
    }
}


public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly StreamWriter _logFileWriter;

    public FileLogger(string categoryName, StreamWriter logFileWriter)
    {
        _categoryName = categoryName;
        _logFileWriter = logFileWriter;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel)
    {
        // Ensure that only information level and higher logs are recorded
        return logLevel >= LogLevel.Trace;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Ensure that only information level and higher logs are recorded
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Get the formatted log message
        var message = formatter(state, exception);

        //Write log messages to text file
        _logFileWriter.WriteLine($"[{logLevel}] {_categoryName}: {message}");
        _logFileWriter.Flush();
    }
}
