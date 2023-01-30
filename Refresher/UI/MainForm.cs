using Eto.Drawing;
using Eto.Forms;

namespace Refresher.UI;

/// <summary>
/// Presents a list of patchers that the user can use to patch for their platform.
/// </summary>
public class MainForm : RefresherForm
{
    public MainForm() : base(string.Empty, new Size(384, -1))
    {
        this.Content = new Button((_,_) => this.ShowChild<FilePatchForm>()) {Text = "File Patch (using a .ELF)"};
    }
}