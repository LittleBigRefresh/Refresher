using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Eto.Drawing;
using Eto.Forms;
using FluentFTP.Exceptions;
using Refresher.Core;
using Refresher.Core.Accessors;
using Refresher.Core.Patching;
using Refresher.Core.Verification;
using Refresher.Core.Patching;
using Refresher.UI.Items;
using SCEToolSharp;
using Sentry;

namespace Refresher.UI;

public abstract class IntegratedPatchForm : PatchForm<EbootPatcher>
{
    protected readonly DropDown GameDropdown;
    protected readonly TextBox? OutputField;
    
    private string _tempFile;
    private string? _usrDir;

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
    
    protected void HandleFtpError(Exception exception, string action)
    {
        this.FailVerify($"An error occurred while {action}: {exception.Message}\n\nTry again in a couple minutes to let things 'cool off'. Sometimes the PS3's kernel just likes to throw a fit.");
    }
    
    protected virtual void PathChanged(object? sender, EventArgs ev)
    {
        if (!(this.Accessor?.Available ?? false))
        {
            State.Logger.LogWarning(LogType.IntegratedPatchForm, "Suppressing path change because accessor is unavailable");
            return;
        }

        State.Logger.LogDebug(LogType.IntegratedPatchForm, $"Path changed, using accessor {this.Accessor.GetType().Name}");
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
            catch (Exception e) when (e is IOException or FtpException or TimeoutException)
            {
                this.HandleFtpError(e, "downloading a game's icon");
                return;
            }
            catch (NotSupportedException)
            {
                // Failed to set image for NPEB01899: System.NotSupportedException: No imaging component suitable to complete this operation was found.
                // ignore for now
            }
            catch (FormatException)
            {
                // Failed to set image for BLUS31426: System.IO.FileFormatException: The image format is unrecognized.
                // also ignore for now

                // FileFormatException seems to not exist, but this page mentions it extending FormatException:
                // https://learn.microsoft.com/en-us/dotnet/api/system.io.fileformatexception
            }
            catch(Exception e)
            {
                State.Logger.LogWarning(InfoRetrieval, $"Failed to set image for {game}: {e}");
                SentrySdk.CaptureException(e);
            }

            string sfoPath = Path.Combine(gamePath, "PARAM.SFO");
            ParamSfo? sfo = null;
            try
            {
                if (this.Accessor.FileExists(sfoPath))
                {
                    Stream sfoStream = this.Accessor.OpenRead(sfoPath);
                    
                    sfo = new ParamSfo(sfoStream);
                    item.Version = "01.00";
                    if (sfo.Table.TryGetValue("APP_VER", out object? value))
                    {
                        string? appVersion = value.ToString();
                        if (appVersion != null)
                            item.Version = appVersion;
                    }
                    else
                    {
                        State.Logger.LogWarning(InfoRetrieval, $"Could not find APP_VER for {game}. Defaulting to 01.00.");
                    }
                    
                    item.Text = $"{sfo.Table["TITLE"]} [{game} {item.Version}]";
                    
                    State.Logger.LogDebug(InfoRetrieval, $"Processed {game}'s PARAM.SFO file. text:\"{item.Text}\" version:\"{item.Version}\"");
                }
                else
                {
                    State.Logger.LogWarning(InfoRetrieval, $"No PARAM.SFO exists for {game} (path should be '{sfoPath}')");
                }
            }
            catch (Exception e) when (e is IOException or FtpException or TimeoutException)
            {
                this.HandleFtpError(e, "downloading a game's PARAM.SFO file");
                return;
            }
            catch (EndOfStreamException)
            {
                State.Logger.LogError(InfoRetrieval, $"Couldn't load {game}'s PARAM.SFO because the file was incomplete.");
            }
            catch(Exception e)
            {
                item.Text = $"Unknown PARAM.SFO [{game}]";
                
                State.Logger.LogError(InfoRetrieval, $"Couldn't load {game}'s PARAM.SFO: {e}");
                if (sfo != null)
                {
                    State.Logger.LogDebug(InfoRetrieval, $"PARAM.SFO version:{sfo.Version} dump:");
                    foreach ((string? key, object? value) in sfo.Table)
                    {
                        State.Logger.LogDebug(InfoRetrieval, $"  '{key}' = '{value}'");
                    }
                }
                else
                {
                    State.Logger.LogWarning(InfoRetrieval, "PARAM.SFO was not read, can't dump");
                }
                
                SentrySdk.CaptureException(e);
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
            State.Logger.LogWarning(LogType.IntegratedPatchForm, "Game was null, bailing");
            return;
        }
        
        if (this.Accessor == null)
        {
            State.Logger.LogWarning(LogType.IntegratedPatchForm, "Accessor was null, bailing");
            return;
        }
        
        State.Logger.LogInfo(LogType.IntegratedPatchForm, $"Game changed to TitleID '{game.TitleId}'");

        this._usrDir = Path.Combine("game", game.TitleId, "USRDIR");
        string ebootPath = Path.Combine(this._usrDir, "EBOOT.BIN.ORIG"); // Prefer original backup over active copy
        
        try
        {
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
        }
        catch (Exception e) when (e is IOException or FtpException or TimeoutException)
        {
            this.HandleFtpError(e, "finding the EBOOT to use");
            return;
        }
        
        string downloadedFile;
        
        try
        {
            downloadedFile = this.Accessor.DownloadFile(ebootPath);
        }
        catch (Exception e) when (e is IOException or FtpException or TimeoutException)
        {
            this.HandleFtpError(e, "downloading the game's EBOOT");
            return;
        }
        
        this.LogMessage($"Downloaded EBOOT Path: {downloadedFile}");
        if (!File.Exists(downloadedFile))
        {
            this.FailVerify("Could not find the EBOOT we downloaded. This is likely a bug. Patching cannot continue.", clear: false);
            return;
        }
        
        try
        {
            // if this is a NP game then download the RIF for the right content ID, disc copies don't need anything else
            if (game.TitleId.StartsWith('N'))
            {
                State.Logger.LogInfo(LogType.IntegratedPatchForm, "Digital game detected, trying to download license file");
                this.DownloadLicenseFile(downloadedFile, game);
            }
        }
        catch (Exception e) when (e is IOException or FtpException or TimeoutException)
        {
            this.HandleFtpError(e, "downloading the game's license file");
            return;
        }

        this._tempFile = Path.GetTempFileName();
        
        State.Logger.LogInfo(Crypto, "Decrypting...");
        LibSceToolSharp.Decrypt(downloadedFile, this._tempFile);
        // HACK: scetool doesn't give us result codes, check if the file has been written to instead
        if (new FileInfo(this._tempFile).Length == 0)
        {
            State.Logger.LogWarning(LogType.IntegratedPatchForm, "Decryption failed on TitleID " + game.TitleId);
            // before we completely fail, let's check if we're a disc game
            // some weird betas like LBP HUB require a license despite having a disc titleid
            if (game.TitleId.StartsWith('B'))
            {
                State.Logger.LogInfo(Crypto, "Disc game detected - trying to gather a license as a workaround for LBP Hub");
                
                try
                {
                    this.DownloadLicenseFile(downloadedFile, game);
                }
                catch (Exception e) when (e is IOException or FtpException or TimeoutException)
                {
                    this.HandleFtpError(e, "downloading the game's license file (with Hub workaround)");
                    return;
                }
                
                LibSceToolSharp.Decrypt(downloadedFile, this._tempFile);
            }

            if (new FileInfo(this._tempFile).Length == 0)
            {
                State.Logger.LogError(Crypto, "Still couldn't decrypt.");
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
        State.Logger.LogInfo(Crypto, $"Downloading license file for TitleID {game.TitleId} (from eboot @ {ebootPath})");
        string? contentId = LibSceToolSharp.GetContentId(ebootPath)?.TrimEnd('\0');

        if (contentId == null)
        {
            State.Logger.LogWarning(Crypto, "Couldn't get the content ID from the EBOOT.");
            return;
        }
        
        this._cachedContentIds[game.TitleId] = contentId;
        
        State.Logger.LogDebug(Crypto, $"ContentID for {game.TitleId} is {contentId}");

        string licenseDir = Path.Join(Path.GetTempPath(), "refresher-" + Random.Shared.Next());
        Directory.CreateDirectory(licenseDir);
        
        if (this.Accessor == null)
        {
            throw new InvalidOperationException("The patch accessor was somehow null while trying to download the game's license.");
        }

        foreach (string user in this.Accessor.GetDirectoriesInDirectory(Path.Combine("home")))
        {
            bool found = false;
            
            State.Logger.LogDebug(Crypto, $"Checking all license files in {user}");
            string exdataFolder = Path.Combine(user, "exdata");

            if (!this.Accessor.DirectoryExists(exdataFolder))
            {
                State.Logger.LogDebug(Crypto, $"Exdata folder doesn't exist for user {user}, skipping...");
                continue;
            }
            
            foreach (string licenseFile in this.Accessor.GetFilesInDirectory(exdataFolder))
            {
                //If the license file does not contain the content ID in its path, skip it
                if (!licenseFile.Contains(contentId) && !licenseFile.Contains(game.TitleId))
                    continue;
                
                State.Logger.LogDebug(Crypto, $"Found compatible rap: {licenseFile}");

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

                State.Logger.LogInfo(Crypto, $"Downloaded compatible license file {licenseFile}.");

                found = true;
            }

            if (found) 
                break;
        }
            
        //If we are using the console patch accessor, fill out the IDPS patch file.
        if (this.Accessor is ConsolePatchAccessor consolePatchAccessor)
        {
            byte[]? idps = consolePatchAccessor.IdpsFile.Value;
            if (idps == null)
            {
                State.Logger.LogError(IDPS, "Can't set IDPS.");
                this.FailVerify("Couldn't retrieve the IDPS used for encryption from the PS3. The log may have more details.");
            }
            else
            {
                State.Logger.LogInfo(IDPS, "Got the IDPS.");
                LibSceToolSharp.SetIdpsKey(idps);
            }
        }

        LibSceToolSharp.SetRifPath(licenseDir);
        LibSceToolSharp.SetRapDirectory(licenseDir);
    }
    
    public override void CompletePatch(object? sender, EventArgs e) {
        if (!(this.Accessor?.Available ?? false))
        {
            State.Logger.LogWarning(LogType.IntegratedPatchForm, "Suppressing patch completion because accessor is unavailable");
            MessageBox.Show("Couldn't complete the patch because we couldn't reach the console. Try restarting Refresher.", MessageBoxType.Warning);
            return;
        }
        
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
        string destination = Path.Combine(this._usrDir!, destinationFile);

        // if we're replacing the executable, back it up to EBOOT.BIN.ORIG before we do so
        if (this.ShouldReplaceExecutable)
        {
            string backup = destination + ".ORIG";
            
            try
            {
                if (!this.Accessor.FileExists(backup))
                    this.Accessor.CopyFile(destination, backup);
            }
            catch (Exception exception) when (exception is IOException or FtpException)
            {
                this.HandleFtpError(exception, "making a backup of the EBOOT");
                return;
            }
        }
        
        try
        {
            if (this.Accessor.FileExists(destination))
                this.Accessor.RemoveFile(destination);
        }
        catch (Exception exception) when (exception is IOException or FtpException)
        {
            this.HandleFtpError(exception, "removing the original EBOOT");
            return;
        }

        // wait a second for the ps3 to calm down
        Thread.Sleep(1000); // TODO: don't. block. the. main. thread.
        
        try
        {
            this.Accessor.UploadFile(fileToUpload, destination);
        }
        catch (Exception exception) when (exception is IOException or FtpException)
        {
            this.HandleFtpError(exception, "applying the EBOOT patch");
            return;
        }
        
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
        if (this._usrDir == null)
        {
            MessageBox.Show("Please select a game before trying to revert the EBOOT.", "Game not selected", MessageBoxType.Error);
            return;
        }

        string eboot = Path.Combine(this._usrDir, "EBOOT.BIN");
        string ebootOrig = Path.Combine(this._usrDir, "EBOOT.BIN.ORIG");

        try
        {
            if (!this.Accessor.FileExists(ebootOrig))
            {
                MessageBox.Show("Cannot revert EBOOT since the original EBOOT does not exist. We're sorry for your loss.", MessageBoxType.Error);
                return;
            }

            if (this.Accessor.FileExists(eboot))
                this.Accessor.RemoveFile(eboot);

            this.Accessor.CopyFile(ebootOrig, eboot);
        }
        catch (Exception exception) when (exception is IOException or FtpException)
        {
            this.HandleFtpError(exception, "reverting the EBOOT");
            return;   
        }

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