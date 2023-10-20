using Eto;
using Eto.Forms;
using Refresher.Accessors;

namespace Refresher.UI;

public class EmulatorPatchForm : IntegratedPatchForm
{
    private FilePicker _folderField = null!;

    public EmulatorPatchForm() : base("RPCS3 Patch")
    {
        this._folderField.FileAction = FileAction.SelectFolder;
        this._folderField.FilePathChanged += this.PathChanged;

        // RPCS3 builds for Windows are portable
        // TODO: Cache the last used location for easier entry
        if (!OperatingSystem.IsWindows())
        {
            // ~/.config/rpcs3/dev_hdd0
            string folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rpcs3", "dev_hdd0");
            if (Directory.Exists(folder))
            {
                this._folderField.FilePath = folder;
                this.PathChanged(this, EventArgs.Empty);
                this.LogMessage("RPCS3's path has been detected automatically! You do not need to change the path.");
            }
        }
    }

    public override void Guide(object? sender, EventArgs e)
    {
        this.OpenUrl("https://docs.littlebigrefresh.com/patching/rpcs3");
    }

    protected override void PathChanged(object? sender, EventArgs ev)
    {
        this.Accessor = new EmulatorPatchAccessor(this._folderField.FilePath);
        base.PathChanged(sender, ev);
    }

    protected override TableRow AddRemoteField()
    {
        return AddField("RPCS3 dev_hdd0 folder", out this._folderField);
    }

    protected override bool NeedsResign => false;
    protected override bool ShouldReplaceExecutable => false;
}