using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Patching;
using Refresher.Core.Patching;

namespace Refresher.UI;

public class PSPSetupForm : PatchForm<PSPPatcher>
{
    private DropDown _pspDrive;
    
    protected override TableLayout FormPanel { get; }
    
    public PSPSetupForm() : base("PSP Setup")
    {
        this.Patcher = new PSPPatcher();
        
        this.FormPanel = new TableLayout(new List<TableRow>
        {
            AddField("PSP Drive", out this._pspDrive),
            AddField("Server URL", out this.UrlField),
        });

        this._pspDrive.SelectedKeyChanged += this.Reverify;
        this._pspDrive.SelectedKeyChanged += this.SelectedDriveChange;
        
        DriveInfo[] drives = DriveInfo.GetDrives();
        
        foreach (DriveInfo drive in drives)
        {
            bool isVita = false;
            string pspEmuFolderName = "";

            try
            {
                State.Logger.LogInfo(PSP, $"Checking drive {drive.Name}...");

                // Match for all directories called PSP and PSPEMU
                // NOTE: we do this because the PSP filesystem is case insensitive, and the .NET STL is case sensitive on linux
                List<string> possiblePspMatches = Directory.EnumerateDirectories(drive.RootDirectory.FullName, "PSP", new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    AttributesToSkip = 0
                }).ToList();

                List<string> possiblePsVitaMatches = Directory.EnumerateDirectories(drive.RootDirectory.FullName, "PSPEMU", new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    AttributesToSkip = 0
                }).ToList();

                // If theres no PSP folder or PSPEMU folder,
                if (!possiblePspMatches.Any() && !possiblePsVitaMatches.Any())
                {
                    State.Logger.LogInfo(PSP, $"Drive {drive.Name} has no PSP/PSPEMU folder, ignoring...");
                    
                    //Skip this drive
                    continue;
                }
                
                isVita = possiblePsVitaMatches.Any();
                if(isVita) pspEmuFolderName = possiblePsVitaMatches[0];
            }
            catch(Exception ex)
            {
                State.Logger.LogError(PSP, $"Couldn't check the drive due to an exception: {ex}");
                // If we fail to check dir info, it's probably not mounted in a safe/accessible way
                continue;
            }
            
            //If the drive has a PSP folder, add it to the list
            this._pspDrive.Items.Add(drive.Name + (isVita ? " (PS Vita)" : " (PSP)"), isVita ? pspEmuFolderName : drive.RootDirectory.FullName);
        }

        // If there are any items in the dropdown...
        if (this._pspDrive.Items.Count > 0)
        {
            // ...then select the first item.
            this._pspDrive.SelectedIndex = 0;
        }
        
        this.InitializePatcher();
    }

    public override void Guide(object? sender, EventArgs e)
    {
        this.OpenUrl("https://docs.littlebigrefresh.com/psp");
    }

    private void SelectedDriveChange(object? sender, EventArgs e)
    {
        this.Patcher!.PSPDrivePath = this._pspDrive.SelectedKey;
    }

    public override void CompletePatch(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Setup complete! *Safely* eject your Memory Stick or PSP in your OS, then open the game!", "Success!");
    }
}