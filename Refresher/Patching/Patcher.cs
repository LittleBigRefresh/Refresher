using System.Diagnostics.Contracts;
using Refresher.Verification;

namespace Refresher.Patching;

public class Patcher
{
    public byte[] Data { get; private set; }

    public Patcher(byte[] data)
    {
        this.Data = data;
    }

    /// <summary>
    /// Checks the contents of the EBOOT to verify that it is patchable.
    /// </summary>
    /// <returns>A list of issues and notes about the EBOOT.</returns>
    [Pure]
    public IEnumerable<VerificationMessage> Verify()
    {
        // TODO: check if this is an ELF, correct architecture, URL is in eboot, etc.
        List<VerificationMessage> messages = new();
        
        messages.Add(new VerificationMessage(MessageLevel.Info, "This EBOOT is an LBP2 copy"));
        messages.Add(new VerificationMessage(MessageLevel.Warning, "The EBOOT is for an unknown architecture"));
        messages.Add(new VerificationMessage(MessageLevel.Error, "This error should stop the patch operation"));
        return messages;
    }

    public void PatchUrl(string url)
    {
        
    }
}