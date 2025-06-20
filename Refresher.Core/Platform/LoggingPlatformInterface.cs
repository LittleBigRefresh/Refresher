namespace Refresher.Core.Platform;

public abstract class LoggingPlatformInterface : IPlatformInterface
{
    public virtual void InfoPrompt(string prompt)
    {
        State.Logger.LogInfo(LogType.Platform, prompt);
    }

    public virtual void WarnPrompt(string prompt)
    {
        State.Logger.LogWarning(LogType.Platform, prompt);
    }

    public virtual void ErrorPrompt(string prompt)
    {
        State.Logger.LogError(LogType.Platform, prompt);
    }

    public abstract QuestionResult Ask(string question);
    public virtual void PrepareThread() {}
    public virtual void StopThread() {}
}