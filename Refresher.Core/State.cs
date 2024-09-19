using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using NotEnoughLogs.Sinks;
using Refresher.Core.Logging;

namespace Refresher.Core;

public static class State
{
    public static readonly Logger Logger = InitializeLogger([new ConsoleSink(), new SentryBreadcrumbSink()]);

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
}