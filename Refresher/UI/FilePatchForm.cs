using System.IO.MemoryMappedFiles;
using Eto;
using Eto.Forms;
using Refresher.Core.Patching;

namespace Refresher.UI;

public class FilePatchForm : PatchForm<EbootPatcher>
{
    private readonly FilePicker _inputFileField;
    private readonly FilePicker _outputFileField;
    
    private string? _tempFile;
    private MemoryMappedFile? _mappedFile;

    protected override TableLayout FormPanel { get; }

    public FilePatchForm() : base("File Patch")
    {
        this.FormPanel = new TableLayout(new List<TableRow>
        {
            AddField("Input EBOOT.elf", out this._inputFileField),
            AddField("Server URL", out this.UrlField),
            AddField("Output EBOOT.elf", out this._outputFileField),
        });

        this._inputFileField.FilePathChanged += this.FileUpdated;
        this._outputFileField.FileAction = FileAction.SaveFile;
        
        this.InitializePatcher();
    }
    
    public override void CompletePatch(object? sender, EventArgs e)
    {
        // Warn user if file already exists
        // Technically most file pickers already handle this for us but still
        DialogResult result = DialogResult.Yes;
        if (File.Exists(this._outputFileField.FilePath))
        {
            result = MessageBox.Show("You are overwriting an existing EBOOT. Are you sure you want to do this?",
                MessageBoxButtons.YesNo, MessageBoxType.Warning);
        }

        if (result == DialogResult.Yes)
        {
            this._mappedFile?.Dispose();
            this._mappedFile = null;
            
            File.Move(this._tempFile, this._outputFileField.FilePath, true);
            MessageBox.Show("Successfully patched EBOOT!");
        }

        // Re-initializes patcher so we can patch with the same parameters again
        // Probably slow but prevents crash
        this.FileUpdated(this, EventArgs.Empty);
    }

    protected override void Reset()
    {
        try
        {
            if (this._tempFile != null) File.Delete(this._tempFile);
        }
        catch
        {
            // ignored
        }
        
        this._mappedFile?.Dispose();
        this._mappedFile = null;
    }

    private void FileUpdated(object? sender, EventArgs ev)
    {
        this.CancelAndWaitForTask();
        
        try
        {
            // Create a temp file to store the EBOOT as we work on it
            this._tempFile = Path.GetTempFileName();

            // Copy the input file to the temp file
            File.Copy(this._inputFileField.FilePath, this._tempFile, true);
        }
        catch (Exception e)
        {
            this.FailVerify("Could not create and copy to temporary file.", e);
            return;
        }

        try
        {
            this._mappedFile?.Dispose();
            this._mappedFile = MemoryMappedFile.CreateFromFile(this._tempFile, FileMode.Open, null, 0, MemoryMappedFileAccess.ReadWrite);
            this.Patcher = new EbootPatcher(this._mappedFile.CreateViewStream());
        }
        catch(Exception e)
        {
            this.FailVerify("Could not read data from the input file.", e);
            return;
        }

        this.Reverify(sender, ev);
    }
}