using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using NotEnoughLogs.Sinks;
using Refresher.Core.Logging;

namespace Refresher.Core;

public static class State
{
    public static readonly Logger Logger = InitializeLogger([new ConsoleSink(), new EventSink(), new SentryBreadcrumbSink()]);

    public delegate void RefresherLogHandler(RefresherLog log);
    public static event RefresherLogHandler? Log;

    private static Logger InitializeLogger(IEnumerable<ILoggerSink> sinks)
    {
        // if(Logger != null)
            // Logger.Dispose();

        return new Logger(sinks, new LoggerConfiguration
        {
            Behaviour = new DirectLoggingBehaviour(),
            MaxLevel = LogLevel.Trace,
        });
    }

    internal static void InvokeOnLog(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> content)
    {
        Log?.Invoke(new RefresherLog(level, category.ToString(), content.ToString()));
    }
}