using Eto.Drawing;
using Eto.Forms;

namespace Refresher.UI;

/// <summary>
/// Presents a list of patchers that the user can use to patch for their platform.
/// </summary>
public class MainForm : RefresherForm
{
    public MainForm() : base(string.Empty, new Size(450, -1))
    {
        StackLayout layout;
        this.Content = layout = new StackLayout
        (
            new Label { Text = "Welcome to Refresher! Please pick a patching method to continue." },
            new Button((_, _) => this.ShowChild<FilePatchForm>()) { Text = "File Patch (using a .ELF)" },
            new Button((_, _) => this.ShowChild<EmulatorPatchForm>()) { Text = "RPCS3 Patch" },
            new Button((_, _) => this.ShowChild<ConsolePatchForm>()) { Text = "PS3 Patch" },
            new Button((_, _) => this.ShowChild<PSPSetupForm>()) { Text = "PSP Setup" }
        );

        layout.Spacing = 5;
        layout.HorizontalContentAlignment = HorizontalAlignment.Stretch;
    }
}