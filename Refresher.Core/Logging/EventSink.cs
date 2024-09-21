using NotEnoughLogs;
using NotEnoughLogs.Sinks;

namespace Refresher.Core.Logging;

public class EventSink : ILoggerSink
{
    public void Log(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> content)
    {
        State.InvokeOnLog(level, category, content);
    }

    public void Log(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> format, params object[] args)
    {
        this.Log(level, category, string.Format(format.ToString(), args));
    }
}