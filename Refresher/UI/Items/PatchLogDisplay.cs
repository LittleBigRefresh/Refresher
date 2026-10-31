using Eto.Forms;

namespace Refresher.UI.Items;

public class PatchLogDisplay
{
    public StackLayoutItem StackItem { get; set; }
    public LabelledRow LabelRow { get; set; }
    public ListBox Display { get; set; }

    public PatchLogDisplay()
    {
        this.Display = new ListBox
        {
            Height = -1,
        };
        
        this.StackItem = new((this.LabelRow = new LabelledRow (this.Display = new ListBox
        {
            Height = -1,
        }, "Patch log", "Displays various messages related to patch progress")).Row, VerticalAlignment.Top, true);
    }
}