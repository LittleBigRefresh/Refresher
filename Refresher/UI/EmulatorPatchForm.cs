using System.Diagnostics;
using Eto;
using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Accessors;
using Refresher.UI.Items;

namespace Refresher.UI;

public class EmulatorPatchForm : IntegratedPatchForm
{
    private FilePicker _folderField = null!;
    private Button _filePatchButton = null!;
    
    public EmulatorPatchForm() : base("RPCS3 Patch")
    {
        this._folderField.FileAction = FileAction.SelectFolder;
        this._folderField.FilePathChanged += this.PathChanged;

        this._filePatchButton.Text = "Go to RPCS3 file patching menu (Advanced)";
        this._filePatchButton.Click += (_, _) => this.ShowChild<EmulatorFilePatchForm>();
        
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
        if (this.GameDropdown.SelectedValue is not GameItem game)
        {
            State.Logger.LogError(PatchForm, "Game was null before patch, bailing");
            return;
        }
        
        if (this.Patcher != null)
        {
            this.Patcher.GenerateRpcs3Patch = true;
            this.Patcher.GameVersion = game.Version;
            this.Patcher.Rpcs3PatchFolder = Path.Combine(this._folderField.FilePath, "../patches");
            this.Patcher.GameName = game.Text;
            this.Patcher.TitleId = game.TitleId;
            
            try
            {
                if(!Directory.Exists(this.Patcher.Rpcs3PatchFolder))
                    Directory.CreateDirectory(this.Patcher.Rpcs3PatchFolder);
            }
            catch (Exception ex)
            {
                State.Logger.LogError(PatchForm, $"Exception while trying to create RPCS3 patches folder: {ex}");
            }
        }
    }

    public override void CompletePatch(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Successfully saved the patch to RPCS3! Open the patch manager and search for the game you patched.", "Success!");
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
            AddField("", out this._filePatchButton),
        };
    }

    protected override void GameChanged(object? sender, EventArgs ev)
    {
        base.GameChanged(sender, ev);
        
        if (this.Patcher == null)
        {
            State.Logger.LogError(PatchForm, "Patcher was null, bailing");
            return;
        }
        
        this.Patcher.GenerateRpcs3Patch = true;
        this.Reverify(null, EventArgs.Empty);
    }

    protected override bool NeedsResign => false;
    protected override bool ShouldReplaceExecutable => true;
    protected override bool ShowRevertEbootButton => false;
}