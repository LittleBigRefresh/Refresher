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
        
        this._patcher.PatchUrl(this._urlField.Text);

        MessageBox.Show("Successfully patched EBOOT!");
    }

    private void FileUpdated(object? sender, EventArgs e)
    {
        try
        {
            this._patcher = new Patcher(File.ReadAllBytes(this._inputFileField.FilePath));
        }
        catch
        {
            this._patcher = null;
            this._patchButton.Enabled = false;
            return;
        }

        this.FormUpdated(sender, e);
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