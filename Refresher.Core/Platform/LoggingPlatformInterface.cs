namespace Refresher.Core.Platform;

public abstract class LoggingPlatformInterface : IPlatformInterface
{
    public virtual void InfoPrompt(string prompt)
    {
        RefresherState.Logger.LogInfo(LogType.Platform, prompt);
    }

    public virtual void WarnPrompt(string prompt)
    {
        RefresherState.Logger.LogWarning(LogType.Platform, prompt);
    }

    public virtual void ErrorPrompt(string prompt)
    {
        RefresherState.Logger.LogError(LogType.Platform, prompt);
    }

    public abstract QuestionResult Ask(string question);

    public virtual void OpenUrl(Uri uri)
    {
        RefresherState.Logger.LogInfo(LogType.Platform, $"Opening URL {uri}");
    }

    public virtual void PrepareThread() {}
    public virtual void PrepareStopThread() {}
}