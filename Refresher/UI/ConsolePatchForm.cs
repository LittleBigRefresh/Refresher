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
        this.Accessor = new ConsolePatchAccessor(this._remoteAddress.Text);
        base.PathChanged(sender, ev);
    }

    protected override void GameChanged(object? sender, EventArgs ev)
    {
        base.GameChanged(sender, ev);
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
}