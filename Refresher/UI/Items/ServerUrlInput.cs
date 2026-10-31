using Eto.Forms;

namespace Refresher.UI.Items;

public class ServerUrlInput : ITableRowWrapper
{
    public TableRow Row => this.LabelRow.Row;
    public LabelledRow LabelRow { get; set; }
    public TextBox Input { get; set; }
    public Button AutoDiscoverButton { get; set; }

    public ServerUrlInput(EventHandler<EventArgs> onAutoDiscoverClick)
    {
        this.Input =  new TextBox
        {
            Text = "",
            PlaceholderText = "http://example.com"
        };
        
        this.LabelRow = new(this.Input, "Server URL", "Enter the URL of the server you want to patch to",
            this.AutoDiscoverButton = new Button(onAutoDiscoverClick) { Text = "AutoDiscover" });
    }
}