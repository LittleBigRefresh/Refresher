using System.Diagnostics;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Refresher.Patching;
using Refresher.UI.Items;
using Refresher.Verification;
using SCEToolSharp;

namespace Refresher.UI;

public class EmulatorPatchForm : PatchForm<Patcher>
{
    private readonly FilePicker _folderField;
    private readonly DropDown _gameDropdown;
    private readonly TextBox _outputField;

    private string _tempFile;
    private string _usrDir;
    private string _ebootPath;
    
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

        this.ClientSize = new Size(600, -1);
        this.InitializePatcher();
    }

    private void PathChanged(object? sender, EventArgs ev)
    {
        string path = this._folderField.FilePath;
        this._gameDropdown.Items.Clear();

        string gamesPath = Path.Join(path, "game");
        if (!Directory.Exists(gamesPath)) return;
            
        string[] games = Directory.GetDirectories(Path.Join(path, "game"));
        
        foreach (string gamePath in games)
        {
            string game = Path.GetFileName(gamePath);
            
            // Example TitleID: BCUS98208, must be 9 chars
            if(game.Length != 9) continue; // Skip over profiles/save data/other garbage

            GameItem item = new();
            
            string iconPath = Path.Combine(gamePath, "ICON0.PNG");
            if (File.Exists(iconPath))
            {
                item.Image = new Bitmap(iconPath).WithSize(new Size(64, 64));
            }
            string sfoPath = Path.Combine(gamePath, "PARAM.SFO");
            try
            {
                ParamSfo sfo = new(File.OpenRead(sfoPath));
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

        this._usrDir = Path.Combine(this._folderField.FilePath, "game", game.TitleId, "USRDIR");
        this._ebootPath = Path.Combine(this._usrDir, "EBOOT.BIN");
        
        this.LogMessage("EBOOT Path: " + this._ebootPath);
        if (!File.Exists(this._ebootPath))
        {
            this.FailVerify("Could not find the EBOOT. Patching cannot continue.", clear: false);
            return;
        }

        this._tempFile = Path.GetTempFileName();
        
        LibSceToolSharp.Decrypt(this._ebootPath, this._tempFile);
        
        this.LogMessage($"The EBOOT has been successfully decrypted. It's stored at {this._tempFile}.");
        
        this.Patcher = new Patcher(File.Open(this._tempFile, FileMode.Open, FileAccess.ReadWrite));

        this.Reverify(sender, ev);
    }
    
    public override void CompletePatch(object? sender, EventArgs e)
    {
        string destination = Path.Combine(this._usrDir, $"EBOOT.{this._outputField.Text}.elf");
        
        File.Move(this._tempFile, destination, true);
        MessageBox.Show($"Successfully patched EBOOT! It was saved to '{destination}'.");

        // Re-initialize patcher so we can patch with the same parameters again
        // Probably slow but prevents crash
        this.GameChanged(this, EventArgs.Empty);
    }
}