using Eto.Forms;
using Refresher.Accessors;

namespace Refresher.UI;

public class ConsolePatchForm : IntegratedPatchForm
{
    private TextBox _remoteAddress = null!;
    
    public ConsolePatchForm() : base("PS3 Patch")
    {}

    protected override void PathChanged(object? sender, EventArgs ev)
    {
        this.InitializePatchAccessor();
        base.PathChanged(sender, ev);
        this.DisposePatchAccessor();
    }

    protected override void GameChanged(object? sender, EventArgs ev)
    {
        this.InitializePatchAccessor();
        base.GameChanged(sender, ev);
        this.DisposePatchAccessor();
    }

    public override void CompletePatch(object? sender, EventArgs e)
    {
        this.InitializePatchAccessor();
        base.CompletePatch(sender, e);
        this.DisposePatchAccessor();
    }

    protected override void RevertToOriginalExecutable(object? sender, EventArgs e)
    {
        this.InitializePatchAccessor();
        base.RevertToOriginalExecutable(sender, e);
        this.DisposePatchAccessor();
    }

    private void InitializePatchAccessor()
    {
        this.DisposePatchAccessor();
        this.Accessor = new ConsolePatchAccessor(this._remoteAddress.Text);
    }

    private void DisposePatchAccessor()
    {
        if (this.Accessor is IDisposable disposable)
            disposable.Dispose();
    }

    protected override TableRow AddRemoteField()
    {
        return AddField("PS3's IP", out this._remoteAddress, new Button(this.PathChanged) { Text = "Connect" });
    }
    
    public override void Guide(object? sender, EventArgs e)
    {
        this.OpenUrl("https://littlebigrefresh.github.io/Docs/patching/ps3");
    }

    protected override bool NeedsResign => true;
    protected override bool ShouldReplaceExecutable => true;

    protected override bool HasGameSelection => true;

    protected override bool PatchesFile => true;
}