using Eto.Drawing;
using Eto.Forms;
using Refresher.UI.Items;

namespace Refresher.UI;

public class ServerSelector : RefresherForm
{
    private static readonly List<Preset> Presets = new()
    {
        new Preset("LittleBigRefresh", "http://refresh.jvyden.xyz:2095/lbp"),
        new Preset("Local Server", "http://localhost:10061/lbp"),
    };

    public ListBox SelectionBox;

    public ServerSelector() : base("Server Selector", new Size(300, 200))
    {
        this.SelectionBox = new();

        foreach (Preset preset in Presets)
        {
            this.SelectionBox.Items.Add(preset.Name, preset.Url);
        }

        this.SelectionBox.Activated += (_, _) => this.Close();

        this.Content = this.SelectionBox;
    }
}