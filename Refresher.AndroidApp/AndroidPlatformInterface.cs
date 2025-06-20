using Android.OS;
using Refresher.Core.Platform;

namespace Refresher.AndroidApp;

public class AndroidPlatformInterface : LoggingPlatformInterface
{
    private void GenericPrompt(string prompt, string type)
    {
        ManualResetEvent latch = new(false);
        
        PipelineActivity.Instance.RunOnUiThread(() =>
        {
            new AlertDialog.Builder(PipelineActivity.Instance)
                .SetTitle($"Refresher {type}")?
                .SetMessage(prompt)?
                .SetNeutralButton("OK", (_, _) =>
                {
                    latch.Set();
                })?
                .Show();
        });

        latch.WaitOne();
    }
    
    public override void InfoPrompt(string prompt)
    {
        base.InfoPrompt(prompt);
        this.GenericPrompt(prompt, string.Empty);
    }

    public override void WarnPrompt(string prompt)
    {
        base.WarnPrompt(prompt);
        this.GenericPrompt(prompt, "Warning");
    }

    public override void ErrorPrompt(string prompt)
    {
        base.ErrorPrompt(prompt);
        this.GenericPrompt(prompt, "Error");
    }

    public override QuestionResult Ask(string question)
    {
        QuestionResult result = QuestionResult.No;
        ManualResetEvent latch = new(false);

        PipelineActivity.Instance.RunOnUiThread(() =>
        {
            new AlertDialog.Builder(PipelineActivity.Instance)
                .SetTitle("Refresher")?
                .SetMessage(question)?
                .SetCancelable(false)?
                .SetPositiveButton("Yes", (_, _) =>
                {
                    result = QuestionResult.Yes;
                    latch.Set();
                })?
                .SetNegativeButton("No", (_, _) =>
                {
                    result = QuestionResult.No;
                    latch.Set();
                })?
                .Show();
        });

        // Block current thread (NOT UI thread!)
        latch.WaitOne();

        return result;
    }

    public override void PrepareThread()
    {
        if(Looper.MyLooper() == null)
            Looper.Prepare();
    }

    public override void PrepareStopThread()
    {
        Looper.MyLooper()?.QuitSafely();
    }
}