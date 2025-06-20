namespace Refresher.Core.Platform;

public interface IPlatformInterface
{
    public void InfoPrompt(string prompt);
    public void WarnPrompt(string prompt);
    public void ErrorPrompt(string prompt);
    public QuestionResult Ask(string question);
    public void PrepareThread();
    public void StopThread();
}