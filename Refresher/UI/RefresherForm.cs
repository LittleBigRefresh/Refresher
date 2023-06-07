using Eto.Drawing;
using Eto.Forms;

namespace Refresher.UI;

/// <summary>
/// Helper abstract class of <see cref="Form"/> to help with titles and apply styling.
/// </summary>
public abstract class RefresherForm : Form
{
    protected RefresherForm(string subtitle, Size size, bool padBottom = true)
    {
        this.Title = "Refresher";
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            this.Title += " - " + subtitle;
        }

        this.ClientSize = size;
        this.Padding = new Padding(10, 10, 10, padBottom ? 10 : 0);
        
        this.Icon = Icon.FromResource("refresher.ico");
    }

    /// <summary>
    /// Shows a child <see cref="RefresherForm"/>.
    /// </summary>
    /// <param name="close">Should the parent window be closed after the child is shown? Defaults to true.</param>
    /// <typeparam name="TForm">The child <see cref="RefresherForm"/> to show.</typeparam>
    protected void ShowChild<TForm>(bool close = true) where TForm : RefresherForm, new()
    {
        TForm form = new();
        form.Show();
        
        if(close) this.Visible = false;
    }
}