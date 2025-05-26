using Eto.Forms;
using Refresher.UI.Items;

namespace Refresher.Extensions;

public static class ControlExtensions
{
    public static string GetUserInput(this Control control)
    {
        return control switch
        {
            TextControl textControl => textControl.Text,
            FilePicker filePicker => filePicker.FilePath,
            DropDown dropDown => (dropDown.SelectedValue as GameItem)?.TitleId ?? string.Empty,
            _ => throw new ArgumentOutOfRangeException(control.GetType().Name),
        };
    }
}