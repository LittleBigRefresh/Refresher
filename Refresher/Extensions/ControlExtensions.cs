using Eto.Forms;

namespace Refresher.Core.Extensions;

public static class ControlExtensions
{
    public static string GetUserInput(this Control control)
    {
        return control switch
        {
            TextControl textControl => textControl.Text,
            FilePicker filePicker => filePicker.FilePath,
            _ => throw new ArgumentOutOfRangeException(control.GetType().Name),
        };
    }
}