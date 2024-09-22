using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using NotEnoughLogs.Sinks;
using Refresher.Core.Logging;

namespace Refresher.Core;

public static class State
{
    public static Logger Logger { get; private set; } = null!;

    public delegate void RefresherLogHandler(RefresherLog log);
    public static event RefresherLogHandler? Log;

    public static void InitializeLogger(IEnumerable<ILoggerSink> sinks)
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        Logger?.Dispose();

        Logger = new Logger(sinks, new LoggerConfiguration
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