namespace Refresher.Verification;

public struct Message
{
    public readonly MessageLevel Level;
    public readonly string Content;

    public Message(MessageLevel level, string content)
    {
        this.Level = level;
        this.Content = content;
    }
}