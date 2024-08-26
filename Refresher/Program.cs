using System.Diagnostics;
using CommandLine;
using Eto.Forms;
using Refresher.CLI;
using Refresher.Patching;
using Refresher.UI;
using Sentry;

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
            Log("Launching in CLI mode");
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(CLI.CommandLine.Run);
        }
        else
        {
            Log("Launching in GUI mode");
            App = new Application();
            App.UnhandledException += (_, eventArgs) =>
            {
                Exception ex = (Exception)eventArgs.ExceptionObject;

                if (ex is DllNotFoundException)
                {
                    string msg = $"""
                                  Refresher is apparently missing critical components that are required to run.
                                  We believe that this is a problem with your OS. If you find out what exactly this is, please let us know on GitHub or Discord.
                                  The exception details are printed below:

                                  {ex}
                                  """;

                    Console.WriteLine(msg); // print the error to stdout
                    try
                    {
                        MessageBox.Show(msg, "Critical Error");
                    }
                    catch
                    {
                        Console.ReadKey(); // try to pause the command window to show the error if we cant show the messagebox
                    }
                    return;
                }
                
                SentrySdk.CaptureException(ex);
                SentrySdk.Flush();
                MessageBox.Show($"""
                                 There was an unhandled error in Refresher.
                                 This has been automatically reported to us. The exception details has been displayed for further debugging:

                                 {ex}
                                 """,
                    "Critical Error");
                
            };
            
            try
            {
                App.Run(new MainForm());
            }
            catch(Exception ex)
            {
                SentrySdk.CaptureException(ex);
                SentrySdk.Flush();
                MessageBox.Show($"""
                                 There was an unhandled error in Refresher.
                                 This has been automatically reported to us. The exception details has been displayed for further debugging:

                                 {ex}
                                 """, "Critical Error");
            }
            App.Dispose();
            SentrySdk.Flush();
        }
    }
}