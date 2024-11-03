using System.Diagnostics;
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
    
    [Conditional("RELEASE")]
    public static void InitializeSentry()
    {
        SentrySdk.Init(options =>
        {
            // A Sentry Data Source Name (DSN) is required.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
            options.Dsn = "https://23dd5e9654ed9843459a8e2e350ab578@o4506662401146880.ingest.sentry.io/4506662403571712";

            // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
            // This might be helpful, or might interfere with the normal operation of your application.
            // We enable it here for demonstration purposes when first trying Sentry.
            // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
            options.Debug = false;

            // This option is recommended. It enables Sentry's "Release Health" feature.
            options.AutoSessionTracking = true;

            // This option is recommended for client applications only. It ensures all threads use the same global scope.
            // If you're writing a background service of any kind, you should remove this.
            options.IsGlobalModeEnabled = true;

            // This option will enable Sentry's tracing features. You still need to start transactions and spans.
            options.EnableTracing = true;

            options.SendDefaultPii = false; // exclude personally identifiable information
            options.AttachStacktrace = true; // send stack traces for *all* breadcrumbs
        });
    }
}