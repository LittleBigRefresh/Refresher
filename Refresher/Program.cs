using System.Diagnostics;
using System.Reflection;
using CommandLine;
using Eto.Forms;
using Refresher.CLI;
using Refresher.Core;
using Refresher.Core.Pipelines;
using Refresher.UI;

namespace Refresher;

#nullable disable

public class Program
{
    public static Application App;

    [Conditional("RELEASE")]
    private static void InitializeSentry()
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

    public static void Log(string message, string category = "", BreadcrumbLevel level = default)
    {
        SentrySdk.AddBreadcrumb(message, category, level: level);
        Console.WriteLine($"[{level}] [{category}] {message}");
    }
    
    [STAThread]
    public static void Main(string[] args)
    {
        InitializeSentry();
        
        if (args.Length > 0)
        {
            State.Logger.LogInfo(OSIntegration, "Launching in CLI mode");
            
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                Exception ex = (Exception)eventArgs.ExceptionObject;
                ReportUnhandledException(ex, false);
            };
            
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(CLI.CommandLine.Run);
        }
        else
        {
            State.Logger.LogInfo(OSIntegration, "Launching in GUI mode");
            try
            {
                App = new Application();
            }
            catch (Exception e)
            {
                ReportUnhandledException(e, false);
                Environment.Exit(-1);
            }

            App.UnhandledException += (_, eventArgs) =>
            {
                Exception ex = (Exception)eventArgs.ExceptionObject;
                ReportUnhandledException(ex, true);
            };
            
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                Exception ex = (Exception)eventArgs.ExceptionObject;
                ReportUnhandledException(ex, true);
            };
            
            try
            {
                App.Run(new MainForm());
            }
            catch(Exception ex)
            {
                SentrySdk.CaptureException(ex);
                SentrySdk.Flush();
                TryShowException(ex, true);
            }
            App.Dispose();
            SentrySdk.Flush();
        }
    }

    /// <summary>
    /// Try our best to report an exception both to Sentry and to the user before crashing.
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="gui">Whether we're in GUI mode</param>
    private static void ReportUnhandledException(Exception ex, bool gui)
    {
        // handle a bunch of known cases first
        switch (ex)
        {
            case TargetInvocationException invocationException:
            {
                // unwrap invocation exceptions
                ReportUnhandledException(invocationException.InnerException, gui);
                return;
            }
            case TypeInitializationException { TypeName: "SCEToolSharp.LibSceToolSharp" }:
            {
                const string msg = """
                                   libscetool, a critical component of Refresher, failed to initialize.
                                   Please ensure you are using the latest version of Refresher, and that you've picked the correct version for your architecture.
                                   """;

                TryShowMessage(msg, true);
                return;
            }
            case DllNotFoundException:
            {
                string msg = $"""
                              Refresher is apparently missing critical components that are required to run.
                              We believe that this is a problem with your OS. If you find out what exactly this is, please let us know on GitHub or Discord.
                              The exception details are printed below:

                              {ex}
                              """;
                TryShowMessage(msg, gui);
                return;
            }
            case InvalidOperationException operationException:
            {
                // "System.InvalidOperationException: Could not detect platform. Are you missing a platform assembly?"
                // at Eto.Platform.get_Detect()
                if (!operationException.Message.Contains("platform"))
                    break;

                string msg =
                    $"""
                      Refresher was unable to load the GUI backend for your platform. It's possible Refresher doesn't support this platform or you downloaded the wrong build.
                      The exception details are printed below:

                      {ex}
                      """;
                TryShowMessage(msg, gui);
                return;
            }
        }

        // if we got here, we don't recognize the exception, so report it
        SentrySdk.CaptureException(ex);
        SentrySdk.Flush();

        TryShowException(ex, gui);
    }

    /// <summary>
    /// Try our best to display a critical crash message to the user.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="gui">Whether we're in GUI mode</param>
    private static void TryShowMessage(string message, bool gui)
    {
        // always print the error to stdout, no matter what.
        // this is pretty much guaranteed to always work
        Console.WriteLine("\n=====Critical Error=====");
        Console.WriteLine(message);
        Console.WriteLine("========================\n");

        if (!gui) return;
        try
        {
            MessageBox.Show(message, "Critical Error");
        }
        catch
        {
            // the reason we might be here is *because* we can't load the GUI.
            // to handle that case, we show the error in the console instead.
            //
            // on windows, the cmd window might instantly close and make the error unreadable,
            // so let's hold the input buffer open
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static void TryShowException(Exception ex, bool gui)
    {
        string msg = $"""
                      There was an unhandled error in Refresher.
                      This has been automatically reported to us. The exception details has been displayed for further debugging:

                      {ex}
                      """;
        TryShowMessage(msg, gui);
    }
}