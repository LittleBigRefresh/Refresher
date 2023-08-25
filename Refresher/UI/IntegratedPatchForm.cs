using System.Diagnostics;
using Eto.Drawing;
using Eto.Forms;
using Refresher.Accessors;
using Refresher.Patching;
using Refresher.UI.Items;
using Refresher.Verification;
using SCEToolSharp;

namespace Refresher.UI;

public abstract class IntegratedPatchForm : PatchForm<Patcher>
{
    private readonly DropDown _gameDropdown;
    private readonly TextBox _outputField;
    
    private string _tempFile;
    private string _usrDir;

    protected PatchAccessor? Accessor;
    
    protected override TableLayout FormPanel { get; }
    
    protected IntegratedPatchForm(string subtitle) : base(subtitle)
    {
        this.FormPanel = new TableLayout(new List<TableRow>
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            this.AddRemoteField(),
            AddField("Game to patch", out this._gameDropdown),
            AddField("Server URL", out this.UrlField),
            AddField("Identifier (EBOOT.<value>.elf)", out this._outputField),
        });
        
        this._outputField.PlaceholderText = "refresh";
        this._gameDropdown.SelectedValueChanged += this.GameChanged;
        
        this.InitializePatcher();
    }
    
    protected virtual void PathChanged(object? sender, EventArgs ev)
    {
        Debug.Assert(this.Accessor != null);
        this._gameDropdown.Items.Clear();
        
        if (!this.Accessor.DirectoryExists("game")) return;
            
        string[] games = this.Accessor.GetDirectoriesInDirectory("game").ToArray();
        
        foreach (string gamePath in games)
        {
            string game = Path.GetFileName(gamePath);
            
            // Example TitleID: BCUS98208, must be 9 chars
            if(game.Length != 9) continue; // Skip over profiles/save data/other garbage
            if(!game.StartsWith("NP") && !game.StartsWith('B')) continue;

            GameItem item = new();
            
            string iconPath = Path.Combine(gamePath, "ICON0.PNG");
            if (this.Accessor.FileExists(iconPath))
            {
                using Stream iconStream = this.Accessor.OpenRead(iconPath);
                item.Image = new Bitmap(iconStream).WithSize(new Size(64, 64));
            }

            string sfoPath = Path.Combine(gamePath, "PARAM.SFO");
            try
            {
                using Stream sfoStream = this.Accessor.OpenRead(sfoPath);
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

    protected void GameChanged(object? sender, EventArgs ev)
    {
        GameItem? game = this._gameDropdown.SelectedValue as GameItem;
        Debug.Assert(game != null);
        Debug.Assert(this.Accessor != null);

        this._usrDir = Path.Combine("game", game.TitleId, "USRDIR");
        string ebootPath = this.Accessor.DownloadFile(Path.Combine(this._usrDir, "EBOOT.BIN"));
        
        string licenseDir = Path.Join(Path.GetTempPath(), "refresher-" + Random.Shared.Next());
        Directory.CreateDirectory(licenseDir);
        IEnumerable<string> licenseFiles = this.Accessor.GetFilesInDirectory(Path.Combine("home", "00000001", "exdata"));
        
        foreach (string licenseFile in licenseFiles)
        {
            if(!licenseFile.Contains(game.TitleId)) continue;
            string downloadedFile = this.Accessor.DownloadFile(licenseFile);
            File.Move(downloadedFile, Path.Join(licenseDir, Path.GetFileName(licenseFile)));
        }
        
        this.LogMessage($"EBOOT Path: {ebootPath}");
        if (!File.Exists(ebootPath))
        {
            this.FailVerify("Could not find the EBOOT. Patching cannot continue.", clear: false);
            return;
        }

        this._tempFile = Path.GetTempFileName();
        
        LibSceToolSharp.SetRapDirectory(licenseDir);
        LibSceToolSharp.Decrypt(ebootPath, this._tempFile);
        
        this.LogMessage($"The EBOOT has been successfully decrypted. It's stored at {this._tempFile}.");
        
        this.Patcher = new Patcher(File.Open(this._tempFile, FileMode.Open, FileAccess.ReadWrite));

        this.Reverify(sender, ev);
    }
    
    public override void CompletePatch(object? sender, EventArgs e) {
        Debug.Assert(this.Accessor != null);
        string identifier = string.IsNullOrWhiteSpace(this._outputField.Text) ? this._outputField.PlaceholderText : this._outputField.Text;
        
        string destination = Path.Combine(this._usrDir, $"EBOOT.{identifier}.elf");
        
        this.Accessor.UploadFile(this._tempFile, destination);
        MessageBox.Show($"Successfully patched EBOOT! It was saved to '{destination}'.");

        // Re-initialize patcher so we can patch with the same parameters again
        // Probably slow but prevents crash
        this.GameChanged(this, EventArgs.Empty);
    }

    protected abstract TableRow AddRemoteField();
}