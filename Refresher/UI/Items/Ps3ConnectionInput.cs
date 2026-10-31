using Eto.Forms;

namespace Refresher.UI.Items;

public class Ps3ConnectionInput : ITableRowWrapper
{
    public TableRow Row => this.LabelRow.Row;
    public LabelledRow LabelRow { get; set; }
    public TextBox Input { get; set; }

    public Ps3ConnectionInput(EventHandler<EventArgs> onConnect)
    {
        this.Input = new TextBox
        {
            Text = "",
            PlaceholderText = "192.168.1.123"
        };
        
        this.LabelRow = new(this.Input, "PS3 IP Address", "Enter the IP Address of your jailbroken PS3 (requires WebMAN!)",
            new Button(onConnect) { Text = "Connect!" });
    }
}