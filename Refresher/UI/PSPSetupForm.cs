using Eto.Forms;
using Refresher.Patching;

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
            try
            {
                //If theres no PSP folder,
                if (!Directory.Exists(Path.Combine(drive.RootDirectory.FullName, "PSP")))
                {
                    //Skip this drive
                    continue;
                }
            }
            catch
            {
                //If we fail to check dir info, its probably not mounted in a safe/accessible way
                continue;
            }
            
            //If the drive has a PSP folder, add it to the list
            this._pspDrive.Items.Add(drive.Name, drive.RootDirectory.FullName);
        }

        //If there is any items in the dropdown,
        if (this._pspDrive.Items.Count > 0)
        {
            //Select the first item
            this._pspDrive.SelectedIndex = 0;
        }
        
        this.InitializePatcher();
    }

    private void SelectedDriveChange(object? sender, EventArgs e)
    {
        this.Patcher!.PSPDrivePath = this._pspDrive.SelectedKey;
    }

    public override void CompletePatch(object? sender, EventArgs e)
    {
    }
}