using Eto.Drawing;
using Eto.Forms;
using Refresher.Core.Patching.Pipelines.Lbp;
using Refresher.Core.Patching.Steps;
using Refresher.UI.Items;

namespace Refresher.UI.PatchForms;

public class LbpRPCS3ReconfigureForm : PipelineForm
{
    // TODO replace with PipelineController once we move the hardcoded steps from the base Pipeline class (e.g. AutoDiscover, eboot revert),
    // and once we make PipelineController (and superclasses of it) manage all the pipelines we need per form
    protected override Type PipelineType => typeof(PatchworkRPCS3ConfigPipeline);
    protected Rpcs3ConnectionInput ConnectionInput { get; set; }
    protected override Button? ConnectButton => this.ConnectionInput.LabelRow.SideButton;

    public LbpRPCS3ReconfigureForm() : this(null)
    {
        
    }
    
    public LbpRPCS3ReconfigureForm(AutoApplyInformation? autoApply) : base(autoApply)
    {
        // Init and add the GUI components
        this.Content = new StackLayout([
            new((this.ConnectionInput = new Rpcs3ConnectionInput(this.OnConnect)).Row),
            new((this.ServerUrlInput = new ServerUrlInput(this.OnAutoDiscoverClick)).Row),
            new((this.JoinKeyInput = new JoinKeyInput(this.OnCopyJoinKeyClick)).Row),

            (this.LogDisplay = new PatchLogDisplay()).StackItem,
            new((this.ProgressBars = new PatchProgressBars()).Row),

            this.PatchButton = new Button(this.OnPatchButtonClick) { Text = "Apply Changes!" },
            new Button(this.OnViewGuideClick) { Text = "View Guide" },
            new Button(this.OnCopyLogClick) { Text = "Copy Log (for support)" },

        ])
        {
            Padding = new Padding(0, 10, 0, 10),
            Spacing = 5,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Size = new Size(-1, -1),
        };
    }

    // TODO adjust this when we replace StepInputs with the UserEnteredInputs class,
    // which will store all inputs using their own props, instead of a dictionary.
    protected override void AddFormInputsToPipeline()
    {
        if (this.Pipeline == null) return;

        this.Pipeline.Inputs.Add(CommonStepInputs.RPCS3Folder.Id, this.ConnectionInput.DevHdd0Picker.FilePath);
        this.Pipeline.Inputs.Add(CommonStepInputs.ServerUrl.Id, this.ServerUrlInput!.Input.Text);
        this.Pipeline.Inputs.Add(CommonStepInputs.LobbyPassword.Id, this.JoinKeyInput!.Input.Text);
    }
}