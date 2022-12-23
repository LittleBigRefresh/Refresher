using System.IO.MemoryMappedFiles;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Refresher.Patching;
using Refresher.Verification;

namespace Refresher.UI;

public class PatchForm : Form
{
    private readonly FilePicker _inputFileField;
    private readonly FilePicker _outputFileField;
    private readonly Button _patchButton;

    private readonly ListBox _problems;
    private readonly TextBox _urlField;
    
    private Patcher? _patcher;
    private string? _tempFile;
    private MemoryMappedFile? _mappedFile;

    public PatchForm()
    {
        this.Title = "Refresher";
        this.ClientSize = new Size(570, -1);
        this.Padding = new Padding(10, 10, 10, 0);

        this.Content = new Splitter
        {
            Orientation = Orientation.Vertical,

            Panel1 = new TableLayout(new List<TableRow>
            {
                AddField("Input EBOOT.elf", out this._inputFileField),
                AddField("Server URL", out this._urlField),
                AddField("Output EBOOT.elf", out this._outputFileField),
            })
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(0, 0, 0, 10),
            },

            // ReSharper disable once RedundantExplicitParamsArrayCreation
            Panel2 = new StackLayout(new StackLayoutItem[]
            {
                this._problems = new ListBox() { Height = 200 },
                new Button(Guide) { Text = "Guide" },
                this._patchButton = new Button(this.Patch) { Text = "Patch!", Enabled = false },
            })
            {
                Padding = new Padding(0, 10, 0, 0),
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            },
        };

        this._inputFileField.FilePathChanged += this.FileUpdated;
        this._urlField.TextChanged += this.FormUpdated;
        this._outputFileField.FileAction = FileAction.SaveFile;
    }

    private static TableRow AddField<TControl>(string labelText, out TControl control) where TControl : Control, new()
    {
        Label label = new()
        {
            Text = labelText + ':',
            VerticalAlignment = VerticalAlignment.Center,
        };

        return new TableRow(label, control = new TControl());
    }

    private static void Guide(object? sender, EventArgs e)
    {
        MessageBox.Show("No guide exists yet, stay tuned!", MessageBoxType.Warning);
    }

    private void Patch(object? sender, EventArgs e)
    {
        if (!this._patchButton.Enabled) return; // shouldn't happen ever but just in-case
        if (this._patcher == null) return;
        if (this._tempFile == null) return;
        
        this._patcher.PatchUrl(this._urlField.Text);
        
        //TODO: warn the user if they are overwriting the file
        File.Move(this._tempFile, this._outputFileField.FilePath, true);

        MessageBox.Show("Successfully patched EBOOT!");
        
        // Re-initializes patcher so we can patch with the same parameters again
        // Probably slow but prevents crash
        this.FileUpdated(this, EventArgs.Empty);
    }

    private void FailVerify(string reason, bool clear = true)
    {
        if(clear) this._problems.Items.Clear();
        this._problems.Items.Add(reason);
        
        try
        {
            if (this._tempFile != null) File.Delete(this._tempFile);
        }
        catch
        {
            // ignored
        }
        
        this._patcher = null;
        this._patchButton.Enabled = false;
        this._mappedFile?.Dispose();
    }

    private void FileUpdated(object? sender, EventArgs ev)
    {
        try
        {
            //Create a temp file to store the EBOOT as we work on it
            this._tempFile = Path.GetTempFileName();

            //Copy the input file to the temp file
            File.Copy(this._inputFileField.FilePath, this._tempFile, true);
        }
        catch (Exception e)
        {
            this.FailVerify("Could not create and copy to temporary file.\n" + e);
            return;
        }

        try
        {
            this._mappedFile?.Dispose();
            this._mappedFile = MemoryMappedFile.CreateFromFile(this._tempFile, FileMode.Open, null, 0, MemoryMappedFileAccess.ReadWrite);
            this._patcher = new Patcher(this._mappedFile.CreateViewStream());
        }
        catch(Exception e)
        {
            this.FailVerify("Could not read data from the input file.\n" + e);
            return;
        }

        this.FormUpdated(sender, ev);
    }

    private void FormUpdated(object? sender, EventArgs e)
    {
        if (this._patcher == null) return;
        
        this._problems.Items.Clear();
        List<Message> messages = this._patcher.Verify(this._urlField.Text).ToList();

        foreach (Message message in messages) this._problems.Items.Add(message.ToString());

        this._patchButton.Enabled = messages.All(m => m.Level != MessageLevel.Error);
    }
}