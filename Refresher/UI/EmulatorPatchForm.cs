using System.Diagnostics;
using Eto;
using Eto.Forms;
using Refresher.Accessors;
using Refresher.UI.Items;

namespace Refresher.UI;

public class EmulatorPatchForm : IntegratedPatchForm
{
    private FilePicker _folderField = null!;
    private CheckBox _outputRpcs3Patch = null!;
    private TextBox _ppuHash = null!;
    private TextBox _gameVersion = null!;

    public EmulatorPatchForm() : base("RPCS3 Patch")
    {
        this._folderField.FileAction = FileAction.SelectFolder;
        this._folderField.FilePathChanged += this.PathChanged;
        
        this._outputRpcs3Patch.CheckedChanged += this.OutputRpcs3PatchCheckedChanged;
        this._outputRpcs3Patch.Checked = false;
        this.OutputRpcs3PatchCheckedChanged(null, EventArgs.Empty);

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
    
    private void OutputRpcs3PatchCheckedChanged(object? sender, EventArgs e)
    {
        this._ppuHash.Enabled = this._outputRpcs3Patch.Checked ?? false;
        this._gameVersion.Enabled = this._outputRpcs3Patch.Checked ?? false;
        
        if(this.OutputField != null)
            this.OutputField.Enabled = !(this._outputRpcs3Patch.Checked ?? false);
    }

    protected override void BeforePatch(object? sender, EventArgs e)
    {
        if (this.Patcher != null)
        {
            this.Patcher.GenerateRpcs3Patch = true;
            this.Patcher.PpuHash = this._ppuHash.Text;
            this.Patcher.GameVersion = this._gameVersion.Text;
            this.Patcher.Rpcs3PatchFolder = Path.Combine(this._folderField.FilePath, "../patches");
            this.Patcher.GameName = ((GameItem)this.GameDropdown.SelectedValue).Text;
        }
    }

    public override void CompletePatch(object? sender, EventArgs e)
    {
        Debug.Assert(this.Patcher != null);
        
        if (this.Patcher.GenerateRpcs3Patch)
            MessageBox.Show(this, $"Successfully saved patch to the RPCS3! Open the patch manager and search for the game you patched!", "Success!");
        else
            base.CompletePatch(sender, e);
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

    protected override IEnumerable<TableRow> AddFields()
    {
        return new[]
        {
            AddField("Output RPCS3 patch file", out this._outputRpcs3Patch),
            AddField("Game PPU hash", out this._ppuHash),
            AddField("Game Version", out this._gameVersion),
            AddField("RPCS3 dev_hdd0 folder", out this._folderField),
        };
    }

    protected override bool NeedsResign => false;
    protected override bool ShouldReplaceExecutable => false;
}