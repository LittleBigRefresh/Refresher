using NotEnoughLogs;
using NotEnoughLogs.Sinks;

namespace Refresher.Core.Logging;

public class SentryBreadcrumbSink : ILoggerSink
{
    private static BreadcrumbLevel GetLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Critical => BreadcrumbLevel.Critical,
            LogLevel.Error => BreadcrumbLevel.Error,
            LogLevel.Warning => BreadcrumbLevel.Warning,
            LogLevel.Info => BreadcrumbLevel.Info,
            LogLevel.Debug => BreadcrumbLevel.Debug,
            LogLevel.Trace => BreadcrumbLevel.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
        };
    }
    
    public void Log(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> content)
    {
        SentrySdk.AddBreadcrumb(content.ToString(), category.ToString(), level: GetLevel(level));
    }

    public void Log(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> format, params object[] args)
    {
        SentrySdk.AddBreadcrumb(string.Format(format.ToString(), args), category.ToString(), level: GetLevel(level));
    }
}