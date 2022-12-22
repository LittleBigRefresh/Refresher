namespace Refresher.Verification;

public struct VerificationMessage
{
    public readonly MessageLevel Level;
    public readonly string Message;

    public VerificationMessage(MessageLevel level, string message)
    {
        this.Level = level;
        this.Message = message;
    }
}