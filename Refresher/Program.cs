using System.Diagnostics;
using CommandLine;
using Eto.Forms;
using Refresher.CLI;
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
            options.Debug = true;

            // This option is recommended. It enables Sentry's "Release Health" feature.
            options.AutoSessionTracking = true;

            // This option is recommended for client applications only. It ensures all threads use the same global scope.
            // If you're writing a background service of any kind, you should remove this.
            options.IsGlobalModeEnabled = true;

            // This option will enable Sentry's tracing features. You still need to start transactions and spans.
            options.EnableTracing = true;
        });
    }
    
    [STAThread]
    public static void Main(string[] args)
    {
        InitializeSentry();
        
        if (args.Length > 0)
        {
            Console.WriteLine("Launching in CLI mode");
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(CLI.CommandLine.Run);
        }
        else
        {
            Console.WriteLine("Launching in GUI mode");
            App = new Application();
            App.UnhandledException += (sender, eventArgs) =>
            {
                SentrySdk.CaptureException((Exception)eventArgs.ExceptionObject);
                MessageBox.Show($"""
                                 There was an unhandled error in Refresher!
                                 *Please* screenshot this message box and send it to us over GitHub or Discord with details on what you were doing. This is likely a bug in Refresher.

                                 Exception details: {eventArgs.ExceptionObject}
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
                MessageBox.Show($"""
                                 There was an unhandled error in Refresher!
                                 *Please* screenshot this message box and send it to us over GitHub or Discord with details on what you were doing. This is likely a bug in Refresher.

                                 Exception details: {ex}
                                 """, "Critical Error");
            }
            App.Dispose();
        }
    }
}