using System.Diagnostics;
using System.Runtime.InteropServices;
using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Platform;
using Refresher.UI;

namespace Refresher;

public class EtoPlatformInterface : LoggingPlatformInterface
{
    private readonly RefresherForm _form;

    public EtoPlatformInterface(RefresherForm form)
    {
        this._form = form;
    }

    public override void InfoPrompt(string prompt)
    {
        base.InfoPrompt(prompt);
        Application.Instance.Invoke(() => 
        {
            MessageBox.Show(this._form, prompt, "Refresher");
        });
    }

    public override void WarnPrompt(string prompt)
    {
        base.WarnPrompt(prompt);
        Application.Instance.Invoke(() => 
        {
            MessageBox.Show(this._form, prompt, "Refresher", MessageBoxType.Warning);
        });
    }

    public override void ErrorPrompt(string prompt)
    {
        base.ErrorPrompt(prompt);
        Application.Instance.Invoke(() => 
        {
            MessageBox.Show(this._form, prompt, "Refresher", MessageBoxType.Error);
        });
    }

    public override QuestionResult Ask(string question)
    {
        
        State.Logger.LogInfo(Platform, $"Asking user '{question}'...");
        DialogResult result = DialogResult.None;
        Application.Instance.Invoke(() =>
        {
            result = MessageBox.Show(question, "Refresher", MessageBoxButtons.YesNo, MessageBoxType.Question);
        });
        
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        State.Logger.LogInfo(Platform, $"User answered {result.ToString()}");
        return result switch
        {
            DialogResult.Yes => QuestionResult.Yes,
            DialogResult.No => QuestionResult.No,
            _ => throw new UnreachableException(),
        };
    }

    public override void OpenUrl(Uri uri)
    {
        base.OpenUrl(uri);
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
}