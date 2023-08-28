using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private readonly TextBox? _outputField;
    
    private string _tempFile;
    private string _usrDir;

    protected PatchAccessor? Accessor;
    
    protected override TableLayout FormPanel { get; }
    
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected IntegratedPatchForm(string subtitle) : base(subtitle)
    {
        this.FormPanel = new TableLayout(new List<TableRow>
        {
            this.AddRemoteField(),
            AddField("Game to patch", out this._gameDropdown, forceHeight: 56),
            AddField("Server URL", out this.UrlField),
        });

        if (!this.ShouldReplaceExecutable)
        {
            this.FormPanel.Rows.Add(AddField("Identifier (EBOOT.<value>.elf)", out this._outputField));
            this._outputField!.PlaceholderText = "refresh";
        }
        
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
            try
            {
                if (this.Accessor.FileExists(iconPath))
                {
                    using Stream iconStream = this.Accessor.OpenRead(iconPath);
                    item.Image = new Bitmap(iconStream).WithSize(new Size(64, 64));
                }
            }
            catch
            {
                // don't set any image
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

    protected virtual void GameChanged(object? sender, EventArgs ev)
    {
        LibSceToolSharp.Init();
        
        GameItem? game = this._gameDropdown.SelectedValue as GameItem;
        Debug.Assert(game != null);
        Debug.Assert(this.Accessor != null);

        this._usrDir = Path.Combine("game", game.TitleId, "USRDIR");
        string ebootPath = this.Accessor.DownloadFile(Path.Combine(this._usrDir, "EBOOT.BIN"));
        
        string licenseDir = Path.Join(Path.GetTempPath(), "refresher-" + Random.Shared.Next());
        Directory.CreateDirectory(licenseDir);

        // if this is a NP game then download RIFs/RAPs, disc copies don't need anything else
        if (game.TitleId.StartsWith('N'))
        {
            // TODO: the first user might not have the licenses necessary, should download from all users
            IEnumerable<string> licenseFiles = this.Accessor.GetFilesInDirectory(Path.Combine("home", "00000001", "exdata"));
            foreach (string licenseFile in licenseFiles)
            {
                // only download if it contains our game's title id
                // TODO: determine content id directly so we skip dlc licenses
                if(!licenseFile.Contains(game.TitleId)) continue;
                
                string downloadedFile = this.Accessor.DownloadFile(licenseFile);
                File.Move(downloadedFile, Path.Join(licenseDir, Path.GetFileName(licenseFile)));
            }
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
        
        string? identifier = string.IsNullOrWhiteSpace(this._outputField?.Text) ? this._outputField?.PlaceholderText : this._outputField?.Text;
        identifier ??= "";

        string fileToUpload;
        if (this.NeedsResign)
        {
            string encryptedTempFile = Path.GetTempFileName();
            LibSceToolSharp.SetDiscEncryptOptions();
            LibSceToolSharp.Encrypt(this._tempFile, encryptedTempFile);

            fileToUpload = encryptedTempFile;
        }
        else
        {
            fileToUpload = this._tempFile;
        }

        string destinationFile = this.ShouldReplaceExecutable ? "EBOOT.BIN" : $"EBOOT.{identifier}.BIN";
        string destination = Path.Combine(this._usrDir, destinationFile);

        // if we're replacing the executable, back it up to EBOOT.BIN.ORIG before we do so
        if (this.ShouldReplaceExecutable)
        {
            string backup = destination + ".ORIG";
            if (!this.Accessor.FileExists(backup))
                this.Accessor.CopyFile(destination, backup);
        }

        if (this.Accessor.FileExists(destination))
            this.Accessor.RemoveFile(destination);

        // wait a second for the ps3 to calm down
        Thread.Sleep(1000); // TODO: don't. block. the. main. thread.
        
        this.Accessor.UploadFile(fileToUpload, destination);
        MessageBox.Show($"Successfully patched EBOOT! It was saved to '{destination}'.");

        // Re-initialize patcher so we can patch with the same parameters again
        // Probably slow but prevents crash
        this.GameChanged(this, EventArgs.Empty);
    }
    
    public override IEnumerable<Button> AddExtraButtons()
    {
        if (this.ShouldReplaceExecutable)
        {
            yield return new Button(this.RevertToOriginalExecutable) { Text = "Revert EBOOT" };
        }
    }

    protected virtual void RevertToOriginalExecutable(object? sender, EventArgs e)
    {
        if (this.Accessor == null) return;

        string eboot = Path.Combine(this._usrDir, "EBOOT.BIN");
        string ebootOrig = Path.Combine(this._usrDir, "EBOOT.BIN.ORIG");

        if (!this.Accessor.FileExists(ebootOrig))
        {
            MessageBox.Show("Cannot revert EBOOT since the original EBOOT does not exist", MessageBoxType.Error);
            return;
        }
        
        if(this.Accessor.FileExists(eboot))
            this.Accessor.RemoveFile(eboot);
        
        this.Accessor.CopyFile(ebootOrig, eboot);
        MessageBox.Show("The EBOOT has successfully been reverted to its original backup");
    }

    protected abstract TableRow AddRemoteField();
    protected abstract bool NeedsResign { get; }
    protected abstract bool ShouldReplaceExecutable { get; }
}