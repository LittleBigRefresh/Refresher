using Eto;
using Eto.Forms;
using Refresher.Core.Helpers;

namespace Refresher.UI.Items;

public class Rpcs3ConnectionInput : ITableRowWrapper
{
    public TableRow Row => this.LabelRow.Row;
    public LabelledRow LabelRow { get; set; }
    public FilePicker DevHdd0Picker { get; set; }

    public Rpcs3ConnectionInput(EventHandler<EventArgs> onConnect)
    {
        this.DevHdd0Picker = new()
        {
            FilePath = PathHelper.GetDefaultRPCS3DevHdd0Path() ?? "",
            FileAction = FileAction.SelectFolder
        };
        this.DevHdd0Picker.FilePathChanged += onConnect;
        
        this.LabelRow = new(this.DevHdd0Picker, "RPCS3 dev_hdd0 folder", "Select the dev_hdd0 folder of your RPCS3 installation");
    }
}