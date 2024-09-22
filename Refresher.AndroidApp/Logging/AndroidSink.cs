using Android.OS;
using Android.Util;
using NotEnoughLogs;
using NotEnoughLogs.Sinks;

namespace Refresher.AndroidApp.Logging;

public class AndroidSink : ILoggerSink
{
    private Handler _handler = new(Looper.MainLooper!);
    
    public void Log(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> content)
    {
        LogPriority priority = level switch
        {
            LogLevel.Critical => LogPriority.Error,
            LogLevel.Error => LogPriority.Error,
            LogLevel.Warning => LogPriority.Warn,
            LogLevel.Info => LogPriority.Info,
            LogLevel.Debug => LogPriority.Debug,
            LogLevel.Trace => LogPriority.Verbose,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
        };
        
        Android.Util.Log.WriteLine(priority, "Refresher." + category.ToString(), content.ToString());
        string contentStr = content.ToString();
        this._handler.Post(() =>
        {
            Toast.MakeText(Application.Context, contentStr, ToastLength.Short)?.Show();
        });
    }

    public void Log(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> format, params object[] args)
    {
        this.Log(level, category, string.Format(format.ToString(), args));
    }
}