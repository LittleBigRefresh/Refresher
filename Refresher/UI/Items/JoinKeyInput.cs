using Eto.Forms;

namespace Refresher.UI.Items;

public class JoinKeyInput : ITableRowWrapper
{
    public TableRow Row => this.LabelRow.Row;
    public LabelledRow LabelRow { get; set; }
    public TextBox Input { get; set; }

    public JoinKeyInput(EventHandler<EventArgs> onCopyUrlClick)
    {
        this.Input = new TextBox
        {
            Text = "",
            PlaceholderText = "(leave empty to randomize)"
        };
        
        this.LabelRow = new(this.Input, "Join Key", "Enter the Join Key you want to use",
            new Button(onCopyUrlClick) { Text = "Copy as URL" });
    }
}