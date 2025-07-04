﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CommandLine;
using Eto.Forms;
using NotEnoughLogs.Sinks;
using Refresher.CLI;
using Refresher.Core;
using Refresher.Core.Logging;
using Refresher.Core.Pipelines.Lbp;
using Refresher.UI;
using Velopack;
using Velopack.Logging;

namespace Refresher;

#nullable disable

public class Program
{
    public static Application App;
    
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp
            .Build()
            .SetLogger(new ConsoleVelopackLogger())
            .Run();
        
        State.InitializeLogger([new ConsoleSink(), new EventSink(), new SentryBreadcrumbSink()]);
        State.InitializeSentry();
        
        State.Logger.LogInfo(OSIntegration, $"Refresher launched with args [{string.Join(',', args)}] (count: {args.Length})");
        bool isCliInvocation = args.Length > 0 && !UrlAssociationHandler.IsArgsUrl(args) && !UrlAssociationHandler.IsArgsTryingToRegisterAssociations(args);
        
        if (isCliInvocation)
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
            
            // on windows, open a log window when shift is held on startup
            // this will NOT work in rider. it must be run independently or the console will be useless
            if (OperatingSystem.IsWindows() && (Keyboard.Modifiers & Keys.Shift) != 0)
                WindowsConsoleHelper.AllocateConsole();
            
            UrlAssociationHandler.RegisterAssociationIfNotPresent();

            if (UrlAssociationHandler.IsArgsTryingToRegisterAssociations(args))
            {
                App.Dispose();
                return;
            }
            
            try
            {
                State.Logger.LogInfo(OSIntegration, "Launching in GUI mode");

                Form form = UrlAssociationHandler.IsArgsUrl(args) ? FormFromUrl(args[0]) : new MainForm();
                
                App.Run(form);
            }
            catch(Exception ex)
            {
                SentrySdk.CaptureException(ex);
                SentrySdk.Flush();
                TryShowException(ex, true);
            }
            App.Dispose();
            SentrySdk.Flush();
            
            if(WindowsConsoleHelper.OpenedConsole)
                WindowsConsoleHelper.ShowEndBlurb();
        }
    }

    private static Form FormFromUrl(string uriStr)
    {
        if (!Uri.TryCreate(uriStr, UriKind.Absolute, out Uri uri))
            InvalidUrl(uriStr);
        Debug.Assert(uri != null);
        Debug.Assert(uri.Scheme == "refresher");

        // refresher://join/ps3?1234
        string[] urlPath = uri.AbsolutePath.Split('/');
        if (urlPath.Length != 2)
            InvalidUrl(uriStr);
        string value = uri.Query.TrimStart('?');

        string method = uri.Host;
        string target = urlPath[1];
        
        if(string.IsNullOrWhiteSpace(value))
            InvalidUrl(uriStr);

        // MessageBox.Show($"method:'{method}'\ntarget:'{target}'\nvalue:'{value}'");

        AutoApplyInformation info = new();

        switch (method)
        {
            case "join":
                info.AutomaticallyApply = true;
                info.AutomaticallyDiscover = true;
                info.JoinKey = value;
                switch (target)
                {
                    case "ps3":
                        return new PipelineForm<PatchworkPS3ConfigPipeline>(info);
                    case "rpcs3":
                        return new PipelineForm<PatchworkRPCS3ConfigPipeline>(info);
                }
                break;
            case "patch":
                info.AutomaticallyApply = false;
                info.AutomaticallyDiscover = true;
                info.ServerUrl = value;
                switch (target)
                {
                    case "ps3":
                        return new PipelineForm<LbpPS3PatchPipeline>(info);
                    case "rpcs3":
                        return new PipelineForm<LbpRPCS3PatchPipeline>(info);
                }
                break;
            default:
                InvalidUrl(uriStr);
                break;
        }

        throw new UnreachableException();
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
    
    [DoesNotReturn]
    private static void InvalidUrl(string uriStr)
    {
        MessageBox.Show($"Invalid Refresher URL: {uriStr}.\n\nWas it copied or pasted incorrectly?");
        Environment.Exit(1);
    }
}