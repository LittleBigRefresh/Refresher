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

public abstract class IntegratedPatchForm : PatchForm<EbootPatcher>
{
    protected readonly DropDown GameDropdown;
    protected readonly TextBox? OutputField;
    
    private string _tempFile;
    private string _usrDir;

    protected PatchAccessor? Accessor;
    
    protected override TableLayout FormPanel { get; }
    
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected IntegratedPatchForm(string subtitle) : base(subtitle)
    {
        List<TableRow> rows = new();
        rows.AddRange(this.AddFields());
        rows.Add(AddField("Game to patch", out this.GameDropdown, forceHeight: 56));
        rows.Add(AddField("Server URL", out this.UrlField));

        this.GameDropdown.SelectedValueChanged += this.GameChanged;

        if (!this.ShouldReplaceExecutable)
        {
            rows.Add(AddField("Identifier (EBOOT.<value>.elf)", out this.OutputField));
            this.OutputField!.PlaceholderText = "refresh";
        }
        
        this.FormPanel = new TableLayout(rows);
        
        this.InitializePatcher();
    }
    
    protected virtual void PathChanged(object? sender, EventArgs ev)
    {
        Debug.Assert(this.Accessor != null);
        this.GameDropdown.Items.Clear();
        
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
            
            this.GameDropdown.Items.Add(item);
        }
    }

    private readonly Dictionary<string, string> _cachedContentIds = new();
    
    protected virtual void GameChanged(object? sender, EventArgs ev)
    {
        LibSceToolSharp.Init();
        
        GameItem? game = this.GameDropdown.SelectedValue as GameItem;
        Debug.Assert(game != null);
        Debug.Assert(this.Accessor != null);

        this._usrDir = Path.Combine("game", game.TitleId, "USRDIR");
        string ebootPath = Path.Combine(this._usrDir, "EBOOT.BIN");

        if (!this.Accessor.FileExists(ebootPath))
        {
            this.FailVerify("The EBOOT.BIN file does not exist. Try pressing 'Revert EBOOT' to see if that helps.");
            return;
        }
        
        string downloadedFile = this.Accessor.DownloadFile(ebootPath);
        
        this.LogMessage($"Downloaded EBOOT Path: {downloadedFile}");
        if (!File.Exists(downloadedFile))
        {
            this.FailVerify("Could not find the EBOOT we downloaded. This is likely a bug. Patching cannot continue.", clear: false);
            return;
        }

        // if this is a NP game then download the RIF for the right content ID, disc copies don't need anything else
        if (game.TitleId.StartsWith('N'))
        {
            string contentId = LibSceToolSharp.GetContentId(downloadedFile).TrimEnd('\0');
            this._cachedContentIds[game.TitleId] = contentId;

            string licenseDir = Path.Join(Path.GetTempPath(), "refresher-" + Random.Shared.Next());
            Directory.CreateDirectory(licenseDir);

            foreach (string user in this.Accessor.GetDirectoriesInDirectory(Path.Combine("home")))
            {
                Console.WriteLine($"Checking all license files in {user}");
                foreach (string licenseFile in this.Accessor.GetFilesInDirectory(Path.Combine(user, "exdata")))
                {
                    //If the license file does not contain the content ID in its path, skip it
                    if (!licenseFile.Contains(contentId))
                        continue;

                    //If it is a valid content id, lets download that user's exdata
                    string downloadedActDat = this.Accessor.DownloadFile(Path.Combine(user, "exdata", "act.dat"));
                    LibSceToolSharp.SetActDatFilePath(downloadedActDat);

                    //And the license file
                    string downloadedLicenseFile = this.Accessor.DownloadFile(licenseFile);
                    File.Move(downloadedLicenseFile, Path.Join(licenseDir, Path.GetFileName(licenseFile)));

                    Console.WriteLine($"Downloaded license file {licenseFile}.");
                }
            }
            
            //If we are using the console patch accessor, fill out the IDPS patch file.
            if (this.Accessor is ConsolePatchAccessor consolePatchAccessor) 
                LibSceToolSharp.SetIdpsKey(consolePatchAccessor.IdpsFile.Value);

            LibSceToolSharp.SetRifPath(licenseDir);
        }

        this._tempFile = Path.GetTempFileName();
        
        LibSceToolSharp.Decrypt(downloadedFile, this._tempFile);
        // HACK: scetool doesn't give us result codes, check if the file has been written to instead
        if (new FileInfo(this._tempFile).Length == 0)
        {
            this.FailVerify("The EBOOT failed to decrypt. Check the log for more information.");
            return;
        }
        
        this.LogMessage($"The EBOOT has been successfully decrypted. It's stored at {this._tempFile}.");
        
        this.Patcher = new EbootPatcher(File.Open(this._tempFile, FileMode.Open, FileAccess.ReadWrite));
        this.Reverify(sender, ev);
    }
    
    public override void CompletePatch(object? sender, EventArgs e) {
        Debug.Assert(this.Accessor != null);
        
        string? identifier = string.IsNullOrWhiteSpace(this.OutputField?.Text) ? this.OutputField?.PlaceholderText : this.OutputField?.Text;
        identifier ??= "";

        string fileToUpload;
        if (this.NeedsResign)
        {
            GameItem? game = this.GameDropdown.SelectedValue as GameItem;
            Debug.Assert(game != null);
            
            string encryptedTempFile = Path.GetTempFileName();
            if (game.TitleId.StartsWith('N'))
            {
                LibSceToolSharp.SetNpdrmEncryptOptions();
                LibSceToolSharp.SetNpdrmContentId(this._cachedContentIds[game.TitleId]);
            }
            else
            {
                LibSceToolSharp.SetDiscEncryptOptions();
            }
            LibSceToolSharp.Encrypt(this._tempFile, encryptedTempFile);

            fileToUpload = encryptedTempFile;
        }
        else
        {
            fileToUpload = this._tempFile;
        }

        string destinationFile = this.ShouldReplaceExecutable ? "EBOOT.BIN" : this.NeedsResign ? $"EBOOT.{identifier}.BIN" : $"EBOOT.{identifier}.elf";
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
        MessageBox.Show(this, $"Successfully patched EBOOT! It was saved to '{destination}'.", "Success!");

        // Re-initialize patcher so we can patch with the same parameters again
        // Probably slow but prevents crash
        this.GameChanged(this, EventArgs.Empty);
    }
    
    public override IEnumerable<Button> AddExtraButtons()
    {
        if (this.ShouldReplaceExecutable && this.ShowRevertEbootButton)
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
            MessageBox.Show("Cannot revert EBOOT since the original EBOOT does not exist. We're sorry for your loss.", MessageBoxType.Error);
            return;
        }
        
        if(this.Accessor.FileExists(eboot))
            this.Accessor.RemoveFile(eboot);
        
        this.Accessor.CopyFile(ebootOrig, eboot);
        MessageBox.Show("The EBOOT has successfully been reverted to its original backup.");
        this.GameChanged(this, EventArgs.Empty);
    }

    protected abstract IEnumerable<TableRow> AddFields();
    /// <summary>
    /// Whether the target platform requires the executable to be resigned or not
    /// </summary>
    protected abstract bool NeedsResign { get; }
    /// <summary>
    /// Whether the target platform requires the executable to be named <c>EBOOT.BIN</c>
    /// </summary>
    protected abstract bool ShouldReplaceExecutable { get; }
    protected virtual bool ShowRevertEbootButton => true;
}