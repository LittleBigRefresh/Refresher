using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Eto.Drawing;
using Eto.Forms;
using Refresher.Accessors;
using Refresher.Patching;
using Refresher.UI.Items;
using Refresher.Verification;
using SCEToolSharp;
using Sentry;

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
        Program.Log($"Path changed, using accessor {this.Accessor.GetType().Name}");
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
                Stream? iconStream = null;

                if (GameCacheAccessor.IconExistsInCache(game))
                {
                    iconStream = GameCacheAccessor.GetIconFromCache(game);
                }
                else if (this.Accessor.FileExists(iconPath))
                {
                    iconStream = this.Accessor.OpenRead(iconPath);
                    GameCacheAccessor.WriteIconToCache(game, iconStream);
                }

                if (iconStream != null)
                {
                    item.Image = new Bitmap(iconStream).WithSize(new Size(64, 64));
                    iconStream.Dispose();
                }
            }
            catch
            {
                // don't set any image
            }

            string sfoPath = Path.Combine(gamePath, "PARAM.SFO");
            try
            {
                Stream sfoStream;

                if (GameCacheAccessor.GameDataExistsInCache(game))
                {
                    sfoStream = GameCacheAccessor.GetGameDataFromCache(game);
                }
                else
                {
                    sfoStream = this.Accessor.OpenRead(sfoPath);
                    GameCacheAccessor.WriteGameDataToCache(game, sfoStream);
                }
                
                ParamSfo sfo = new(sfoStream);
                item.Version = sfo.Table["APP_VER"].ToString() ?? "";
                item.Text = $"{sfo.Table["TITLE"]} [{game}]";
                
                Program.Log($"Processed {game}'s param.sfo file. text:\"{item.Text}\" version:\"{item.Version}", "SFO");
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
        
        if (this.GameDropdown.SelectedValue is not GameItem game)
        {
            Program.Log("Game was null, bailing", nameof(IntegratedPatchForm));
            return;
        }
        
        if (this.Accessor == null)
        {
            Program.Log("Accessor was null, bailing", nameof(IntegratedPatchForm));
            return;
        }
        
        Program.Log($"Game changed to TitleID '{game.TitleId}'");

        this._usrDir = Path.Combine("game", game.TitleId, "USRDIR");
        string ebootPath = Path.Combine(this._usrDir, "EBOOT.BIN.ORIG"); // Prefer original backup over active copy
        
        // If the backup doesn't exist, use the EBOOT.BIN
        if (!this.Accessor.FileExists(ebootPath))
        {
            this.LogMessage("Couldn't find an original backup of the EBOOT, using active copy. This is not an error.");
            ebootPath = Path.Combine(this._usrDir, "EBOOT.BIN");
            
            // If we land here, then we have no valid patch target without any way to recover.
            // This is very inconvenient for us and the user.
            if (!this.Accessor.FileExists(ebootPath))
            {
                this.FailVerify("The EBOOT.BIN file does not exist, nor does the original backup exist. This usually means you haven't installed any updates for your game.");
                return;
            }
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
            Program.Log("Digital game detected, trying to download license file");
            this.DownloadLicenseFile(downloadedFile, game);
        }

        this._tempFile = Path.GetTempFileName();
        
        Program.Log("Decrypting...");
        LibSceToolSharp.Decrypt(downloadedFile, this._tempFile);
        // HACK: scetool doesn't give us result codes, check if the file has been written to instead
        if (new FileInfo(this._tempFile).Length == 0)
        {
            Program.Log("Decryption failed on TitleID " + game.TitleId);
            // before we completely fail, let's check if we're a disc game
            // some weird betas like LBP HUB require a license despite having a disc titleid
            if (game.TitleId.StartsWith('B'))
            {
                Program.Log("Disc game detected - trying to gather a license as a workaround for LBP Hub");
                this.DownloadLicenseFile(downloadedFile, game);
                LibSceToolSharp.Decrypt(downloadedFile, this._tempFile);
            }

            if (new FileInfo(this._tempFile).Length == 0)
            {
                Program.Log("Still couldn't decrypt.");
                this.FailVerify("The EBOOT failed to decrypt. Check the log for more information.");
                return;
            }
        }
        
        this.LogMessage($"The EBOOT has been successfully decrypted. It's stored at {this._tempFile}.");
        
        this.Patcher = new EbootPatcher(File.Open(this._tempFile, FileMode.Open, FileAccess.ReadWrite));
        this.Reverify(sender, ev);
    }

    private void DownloadLicenseFile(string ebootPath, GameItem game)
    {
        Program.Log($"Downloading license file for TitleID {game.TitleId} (from eboot @ {ebootPath})");
        string contentId = LibSceToolSharp.GetContentId(ebootPath).TrimEnd('\0');
        this._cachedContentIds[game.TitleId] = contentId;
        
        Program.Log($"ContentID for {game.TitleId} is {contentId}");

        string licenseDir = Path.Join(Path.GetTempPath(), "refresher-" + Random.Shared.Next());
        Directory.CreateDirectory(licenseDir);
        
        if (this.Accessor == null)
        {
            throw new InvalidOperationException("The patch accessor was somehow null while trying to download the game's license.");
        }

        foreach (string user in this.Accessor.GetDirectoriesInDirectory(Path.Combine("home")))
        {
            bool found = false;
            
            Program.Log($"Checking all license files in {user}");
            foreach (string licenseFile in this.Accessor.GetFilesInDirectory(Path.Combine(user, "exdata")))
            {
                //If the license file does not contain the content ID in its path, skip it
                if (!licenseFile.Contains(contentId) || licenseFile.Contains(game.TitleId))
                    continue;
                
                Program.Log($"Found compatible rap: {licenseFile}");

                string actDatPath = Path.Combine(user, "exdata", "act.dat");
                    
                //If it is a valid content id, lets download that user's act.dat, if its there
                if (!found && this.Accessor.FileExists(actDatPath))
                {
                    string downloadedActDat = this.Accessor.DownloadFile(actDatPath);
                    LibSceToolSharp.SetActDatFilePath(downloadedActDat);
                }

                //And the license file
                string downloadedLicenseFile = this.Accessor.DownloadFile(licenseFile);
                File.Move(downloadedLicenseFile, Path.Join(licenseDir, Path.GetFileName(licenseFile)));

                Program.Log($"Downloaded license file {licenseFile}.");

                found = true;
            }

            if (found) 
                break;
        }
            
        //If we are using the console patch accessor, fill out the IDPS patch file.
        if (this.Accessor is ConsolePatchAccessor consolePatchAccessor) 
            LibSceToolSharp.SetIdpsKey(consolePatchAccessor.IdpsFile.Value);

        LibSceToolSharp.SetRifPath(licenseDir);
        LibSceToolSharp.SetRapDirectory(licenseDir);
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