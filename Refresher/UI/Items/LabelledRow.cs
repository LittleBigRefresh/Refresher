using Eto.Drawing;
using Eto.Forms;

namespace Refresher.UI.Items;

public class LabelledRow : TableRow, ITableRowWrapper
{
    public TableRow Row { get; set; }
    public Label Label { get; set; }
    public Control Content { get; set; }
    public Button? SideButton { get; set; }

    public LabelledRow(Control content, string labelText, string tooltipText, Button? sideButton = null)
    {
        this.Label = new()
        {
            // line break above label
            Text = labelText + ':',
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = tooltipText,
        };

        this.Content = content;
        this.SideButton = sideButton;

        if (sideButton == null)
        {
            this.Row = new(new TableLayout([
                this.Label,
                this.Content,
            ]));
        }
        else
        {
            this.Row = new(new TableLayout([
                this.Label,
                this.Content,
                // would make more sense if the button was to the right of the text field,
                // but eto sucks and doesn't let me set the text field to take max width or something like that,
                // so it has to be either the right most component (doesn't look great),
                // or it has to be in its own row by itself (decent enough, but not really good imo).
                sideButton,
            ]));
        }
    }
}