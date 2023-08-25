using System.Diagnostics;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Refresher.Accessors;
using Refresher.Patching;
using Refresher.UI.Items;
using Refresher.Verification;
using SCEToolSharp;

namespace Refresher.UI;

public class EmulatorPatchForm : PatchForm<Patcher>
{
    private readonly FilePicker _folderField;
    private readonly DropDown   _gameDropdown;
    private readonly TextBox    _outputField;

    private string _tempFile;
    private string _usrDir;

    private EmulatorPatchAccessor? _accessor;

    protected override TableLayout FormPanel { get; }
    
    public EmulatorPatchForm() : base("RPCS3 Patch")
    {
        this.FormPanel = new TableLayout(new List<TableRow>
        {
            AddField("RPCS3 dev_hdd0 folder", out this._folderField),
            AddField("Game to patch", out this._gameDropdown),
            AddField("Server URL", out this.UrlField),
            AddField("Identifier (EBOOT.<value>.elf)", out this._outputField),
        });

        this._folderField.FileAction = FileAction.SelectFolder;
        this._folderField.FilePathChanged += this.PathChanged;
        
        this._outputField.PlaceholderText = "refresh";

        this._gameDropdown.SelectedValueChanged += this.GameChanged;

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

        this.InitializePatcher();
    }

    private void PathChanged(object? sender, EventArgs ev)
    {
        string path = this._folderField.FilePath;
        this._gameDropdown.Items.Clear();

        this._accessor = new EmulatorPatchAccessor(path);
        
        if (!this._accessor.DirectoryExists("game")) return;
            
        string[] games = this._accessor.GetDirectoriesInDirectory("game").ToArray();
        
        foreach (string gamePath in games)
        {
            string game = Path.GetFileName(gamePath);
            
            // Example TitleID: BCUS98208, must be 9 chars
            if(game.Length != 9) continue; // Skip over profiles/save data/other garbage

            GameItem item = new();
            
            string iconPath = Path.Combine(gamePath, "ICON0.PNG");
            if (this._accessor.FileExists(iconPath))
            {
                using Stream iconStream = this._accessor.OpenRead(iconPath);
                item.Image = new Bitmap(iconStream).WithSize(new Size(64, 64));
            }

            string sfoPath = Path.Combine(gamePath, "PARAM.SFO");
            try
            {
                using Stream sfoStream = this._accessor.OpenRead(sfoPath);
                ParamSfo sfo = new(sfoStream);
                item.Text = $"{sfo.Table["TITLE"]} [{game}]";
            }
            catch
            {
                item.Text = game;
            }

            item.TitleId = game;
            
            this._gameDropdown.Items.Add(item);
        }
    }

    private void GameChanged(object? sender, EventArgs ev)
    {
        GameItem? game = this._gameDropdown.SelectedValue as GameItem;
        Debug.Assert(game != null);
        Debug.Assert(this._accessor != null);

        this._usrDir = Path.Combine("game", game.TitleId, "USRDIR");
        string ebootPath = this._accessor.DownloadFile(Path.Combine(this._usrDir, "EBOOT.BIN"));
        string rapDir = this._accessor.DownloadDirectory(Path.Combine("home", "00000001", "exdata"));
        
        this.LogMessage($"EBOOT Path: {ebootPath}");
        if (!this._accessor.FileExists(ebootPath))
        {
            this.FailVerify("Could not find the EBOOT. Patching cannot continue.", clear: false);
            return;
        }

        this._tempFile = Path.GetTempFileName();
        
        LibSceToolSharp.SetRapDirectory(rapDir);
        LibSceToolSharp.Decrypt(ebootPath, this._tempFile);
        
        this.LogMessage($"The EBOOT has been successfully decrypted. It's stored at {this._tempFile}.");
        
        this.Patcher = new Patcher(File.Open(this._tempFile, FileMode.Open, FileAccess.ReadWrite));

        this.Reverify(sender, ev);
    }
    
    public override void CompletePatch(object? sender, EventArgs e) {
        Debug.Assert(this._accessor != null);
        string identifier = string.IsNullOrWhiteSpace(this._outputField.Text) ? this._outputField.PlaceholderText : this._outputField.Text;
        
        string destination = Path.Combine(this._usrDir, $"EBOOT.{identifier}.elf");
        
        this._accessor.UploadFile(this._tempFile, destination);
        MessageBox.Show($"Successfully patched EBOOT! It was saved to '{destination}'.");

        // Re-initialize patcher so we can patch with the same parameters again
        // Probably slow but prevents crash
        this.GameChanged(this, EventArgs.Empty);
    }

    public override void Guide(object? sender, EventArgs e)
    {
        this.OpenUrl("https://littlebigrefresh.github.io/Docs/patching/rpcs3");
    }
}