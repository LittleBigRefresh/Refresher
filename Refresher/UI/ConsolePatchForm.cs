using System.Net.Sockets;
using System.Reflection;
using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Accessors;
using Refresher.Core.Exceptions;

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
            MessageBox.Show("Please enter a valid IP address. You can usually see this listed under Network settings on your PS3.", "Error");
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
        State.Logger.LogTrace(LogType.Accessor, "Making a new patch accessor");
        try
        {
            this.Accessor = new ConsolePatchAccessor(this._remoteAddress.Text.Trim());
        }
        catch (FTPConnectionFailureException)
        {
            MessageBox.Show("Could not connect to the FTP server likely due to the PS3 rejecting the connection.\nAre you sure the webMAN FTP server is running?", "Error");
            return false;
        }
        catch(TimeoutException)
        {
            MessageBox.Show("The FTP connection timed out while we were waiting for a response from the PS3.\nAre you sure the webMAN FTP server is running?", "Error");
            return false;
        }
        catch(UriFormatException)
        {
            MessageBox.Show("The IP address was unable to be parsed. Are you sure you typed it in correctly?", "Error");
            return false; 
        }
        catch(Exception ex)
        {
            MessageBox.Show($"An unknown error occurred while connecting to the PS3.\n\nException details: {ex}", "Error");
            return false;
        }

        return true;
    }

    private void DisposePatchAccessor()
    {
        State.Logger.LogTrace(LogType.Accessor, "Disposing patch accessor");
        if (this.Accessor is IDisposable disposable)
            disposable.Dispose();
    }

    protected override IEnumerable<TableRow> AddFields()
    {
        return [AddField("PS3's IP", out this._remoteAddress, new Button(this.PathChanged) { Text = "Connect" })];
    }
    
    public override void Guide(object? sender, EventArgs e)
    {
        this.OpenUrl("https://docs.littlebigrefresh.com/ps3");
    }

    protected override bool NeedsResign => true;
    protected override bool ShouldReplaceExecutable => true;
}