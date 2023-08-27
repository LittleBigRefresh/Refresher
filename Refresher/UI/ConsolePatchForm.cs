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

    protected override TableRow AddRemoteField()
    {
        return AddField("PS3's IP", out this._remoteAddress, new Button(this.PathChanged) { Text = "Connect" });
    }

    protected override bool NeedsResign => true;
    protected override bool ShouldReplaceExecutable => true;
}