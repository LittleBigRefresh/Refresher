using Refresher.Verification;

namespace Refresher.Patching;

public interface IPatcher
{
    public List<Message> Verify(string url, bool patchDigest);

    public void Patch(string url, bool patchDigest);
}