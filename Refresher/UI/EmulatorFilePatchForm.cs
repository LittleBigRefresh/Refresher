using System.Diagnostics;
using Eto;
using Eto.Forms;
using Refresher.Accessors;
using Refresher.UI.Items;

namespace Refresher.UI;

public class EmulatorFilePatchForm : IntegratedPatchForm
{
    private FilePicker _folderField = null!;

    public EmulatorFilePatchForm() : base("Advanced RPCS3 Patch")
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
    
    protected override void BeforePatch(object? sender, EventArgs e)
    {
        if (this.Patcher != null)
        {
            this.Patcher.GenerateRpcs3Patch = false;
        }
    }

    public override void Guide(object? sender, EventArgs e)
    {
        this.OpenUrl("https://docs.littlebigrefresh.com/rpcs3");
    }

    protected override void PathChanged(object? sender, EventArgs ev)
    {
        this.Accessor = new EmulatorPatchAccessor(this._folderField.FilePath);
        base.PathChanged(sender, ev);
    }

    protected override IEnumerable<TableRow> AddFields()
    {
        return new[]
        {
            AddField("RPCS3 dev_hdd0 folder", out this._folderField),
        };
    }

    protected override bool NeedsResign => false;
    protected override bool ShouldReplaceExecutable => false;
}