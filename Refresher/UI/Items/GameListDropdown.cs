using Eto.Forms;

namespace Refresher.UI.Items;

public class GameListDropdown : ITableRowWrapper
{
    public TableRow Row => this.LabelRow.Row;
    public LabelledRow LabelRow { get; set; }
    public DropDown Dropdown { get; set; }

    // No content initially, will be manually set by the parent's OnConnect() method
    public GameListDropdown()
    {
        this.Dropdown = new DropDown
        {
            Enabled = false, // only enable when games are downloaded, which will always happen after init
            Height = 56,
        };
        
        this.LabelRow = new(this.Dropdown, "Game", "Select the game you want to patch (only available once you connect)");
    }
}