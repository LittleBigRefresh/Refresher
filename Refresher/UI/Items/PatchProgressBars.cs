using Eto.Drawing;
using Eto.Forms;

namespace Refresher.UI.Items;

public class PatchProgressBars : ITableRowWrapper
{
    public TableRow Row { get; set; }

    public LabelledRow PipelineProgressRow { get; set; }
    public LabelledRow StepProgressRow { get; set; }

    public ProgressBar PipelineProgressBar { get; set; }
    public ProgressBar StepProgressBar { get; set; }

    public PatchProgressBars()
    {
        // TODO use current pipeline name and step name in progress bar labels, and the pipeline state
        // as their tooltip.
        this.PipelineProgressRow = new(this.PipelineProgressBar = new(), "Pipeline progress", "");
        this.StepProgressRow = new(this.StepProgressBar = new(), "Step progress", "");

        this.PipelineProgressBar.Value = 0;
        this.StepProgressBar.Value = 0;

        this.Row = new(new StackLayout([
            new(this.PipelineProgressRow.Row),
            new(this.StepProgressRow.Row),
        ])
        {
            Padding = new Padding(0, 10, 0, 10),
            Spacing = 5,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Size = new Size(-1, -1),
        });
    }
}