using Eto.Drawing;
using Eto.Forms;
using Refresher.Core.Pipelines;

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
        // ReSharper disable once RedundantExplicitParamsArrayCreation
        ([
            new Label { Text = "Welcome to Refresher! Please pick a patching method to continue." },
            new Button((_, _) => this.ShowChild<FilePatchForm>()) { Text = "File Patch (using a .ELF)" },
            this.PipelineButton<RPCS3PatchPipeline>("Patch an RPCS3 game"),
            this.PipelineButton<PS3PatchPipeline>("Patch a PS3 game"),
            #if DEBUG
            new Label { Text = "Debugging options:" },
            this.PipelineButton<ExamplePipeline>("Example Pipeline"),
            #endif
        ]);

        layout.Spacing = 5;
        layout.HorizontalContentAlignment = HorizontalAlignment.Stretch;
    }

    private Button PipelineButton<TPipeline>(string name) where TPipeline : Pipeline, new()
    {
        return new Button((_, _) => this.ShowChild<PipelineForm<TPipeline>>()) { Text = name };
    }
}