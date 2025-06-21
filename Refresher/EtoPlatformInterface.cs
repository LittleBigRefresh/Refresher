using System.Diagnostics;
using System.Runtime.InteropServices;
using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Platform;
using Refresher.UI;

namespace Refresher;

public class EtoPlatformInterface : IPlatformInterface
{
    private readonly RefresherForm _form;

    public EtoPlatformInterface(RefresherForm form)
    {
        this._form = form;
    }

    public void InfoPrompt(string prompt)
    {
        Application.Instance.Invoke(() => 
        {
            State.Logger.LogInfo(Platform, prompt);
            MessageBox.Show(this._form, prompt, "Refresher");
        });
    }

    public void WarnPrompt(string prompt)
    {
        Application.Instance.Invoke(() => 
        {
            State.Logger.LogWarning(Platform, prompt);
            MessageBox.Show(this._form, prompt, "Refresher", MessageBoxType.Warning);
        });
    }

    public void ErrorPrompt(string prompt)
    {
        Application.Instance.Invoke(() => 
        {
            State.Logger.LogError(Platform, prompt);
            MessageBox.Show(this._form, prompt, "Refresher", MessageBoxType.Error);
        });
    }

    public QuestionResult Ask(string question)
    {
        State.Logger.LogInfo(Platform, $"Asking user '{question}'...");
        DialogResult result = MessageBox.Show(question, "Refresher", MessageBoxButtons.YesNo, MessageBoxType.Question);
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        State.Logger.LogInfo(Platform, $"User answered {result.ToString()}");
        return result switch
        {
            DialogResult.Yes => QuestionResult.Yes,
            DialogResult.No => QuestionResult.No,
            _ => throw new UnreachableException(),
        };
    }

    public void OpenUrl(Uri uri)
    {
        string url = uri.ToString();
        
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                throw new PlatformNotSupportedException("Cannot open a URL on this platform.");
        }
        catch (Exception e)
        {
            State.Logger.LogError(OSIntegration, e.ToString());
            MessageBox.Show("We couldn't open your browser due to an error.\n" +
                            $"You can use this link instead: {url}\n\n" +
                            $"Exception details: {e.GetType().Name} {e.Message}", MessageBoxType.Error);
            SentrySdk.CaptureException(e);
        }
        // based off of https://stackoverflow.com/a/43232486
    }

    public void PrepareThread()
    {}

    public void PrepareStopThread()
    {}
}