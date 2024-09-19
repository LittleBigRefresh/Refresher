using Refresher.Core.Verification;

namespace Refresher.Core.Patching;

public interface IPatcher
{
    public List<Message> Verify(string url, bool patchDigest);

    public void Patch(string url, bool patchDigest);
}