using Android.OS;
using Android.Util;
using NotEnoughLogs;
using NotEnoughLogs.Sinks;

namespace Refresher.AndroidApp.Logging;

public class AndroidSink : ILoggerSink
{
    private readonly Handler _handler = new(Looper.MainLooper!);
    
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
        string categoryStr = category.ToString();
        string contentStr = content.ToString();
        this._handler.Post(() =>
        {
            if (priority == LogPriority.Error)
            {
                new AlertDialog.Builder(PipelineActivity.Instance)
                    .SetTitle($"{categoryStr} Error")?
                    .SetMessage(contentStr)?
                    .SetNeutralButton("OK", (_, _) => {})?
                    .Show();
            }
            else if(priority == LogPriority.Warn)
            {
                Toast.MakeText(PipelineActivity.Instance, contentStr, ToastLength.Short)?.Show();
            }
        });
    }

    public void Log(LogLevel level, ReadOnlySpan<char> category, ReadOnlySpan<char> format, params object[] args)
    {
        this.Log(level, category, string.Format(format.ToString(), args));
    }
}