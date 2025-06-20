using System.Diagnostics;
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
}