using System.Net.Sockets;
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
        if (this._remoteAddress.Text.Trim().Length == 0)
        {
            MessageBox.Show("Please input a valid IP!", "Error");
            return;
        }

        if (!this.InitializePatchAccessor()) 
            return;
        base.PathChanged(sender, ev);
        this.DisposePatchAccessor();
    }

    protected override void GameChanged(object? sender, EventArgs ev)
    {
        if (!this.InitializePatchAccessor()) 
            return;
        base.GameChanged(sender, ev);
        this.DisposePatchAccessor();
    }

    public override void CompletePatch(object? sender, EventArgs e)
    {
        if (!this.InitializePatchAccessor()) 
            return;
        base.CompletePatch(sender, e);
        this.DisposePatchAccessor();
    }

    protected override void RevertToOriginalExecutable(object? sender, EventArgs e)
    {
        if (!this.InitializePatchAccessor()) 
            return;
        base.RevertToOriginalExecutable(sender, e);
        this.DisposePatchAccessor();
    }

    private bool InitializePatchAccessor()
    {
        this.DisposePatchAccessor();
        try
        {
            this.Accessor = new ConsolePatchAccessor(this._remoteAddress.Text.Trim());
        }
        catch(TimeoutException)
        {
            MessageBox.Show($"Timed out waiting for response from PS3...\nAre you sure the webMAN FTP server is running?", "Error!");
            return false;
        }
        catch(UriFormatException)
        {
            MessageBox.Show($"Unable to parse IP, make sure you typed it in correctly!", "Error!");
            return false; 
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Unknown error failed while connecting to PS3...\nAre you sure the IP is correct?\n\n{ex}", "Error!");
            return false;
        }

        return true;
    }

    private void DisposePatchAccessor()
    {
        if (this.Accessor is IDisposable disposable)
            disposable.Dispose();
    }

    protected override IEnumerable<TableRow> AddFields()
    {
        return new[] { AddField("PS3's IP", out this._remoteAddress, new Button(this.PathChanged) { Text = "Connect" }) };
    }
    
    public override void Guide(object? sender, EventArgs e)
    {
        this.OpenUrl("https://docs.littlebigrefresh.com/patching/ps3");
    }

    protected override bool NeedsResign => true;
    protected override bool ShouldReplaceExecutable => true;
}