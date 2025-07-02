using Eto.Drawing;
using Eto.Forms;
using System.Runtime.InteropServices;
using Refresher.Core;

namespace Refresher.UI;

/// <summary>
/// Helper abstract class of <see cref="Form"/> to help with titles and apply styling.
/// </summary>
public abstract class RefresherForm : Form
{
    protected RefresherForm(string subtitle, Size size, bool padBottom = true)
    {
        UpdateSubtitle(subtitle);

        this.ClientSize = size;
        // this.AutoSize = true;
        this.Padding = new Padding(10, 10, 10, padBottom ? 10 : 0);
        
        try
        {
            this.Icon = Icon.FromResource("refresher.ico");
        }
        catch(Exception ex)
        {
            // Not very important, so these should just be warnings, not errors
            State.Logger.LogWarning(LogType.RefresherForm, $"Unhandled exception while loading refresher.ico in {this.GetType()}: {ex}");
            
            // attempt to fall back to png icon
            try
            {
                this.Icon = Icon.FromResource("refresher.png");
            }
            catch (Exception ex2)
            {
                State.Logger.LogWarning(LogType.RefresherForm, $"Unhandled exception while loading refresher.png in {this.GetType()}: {ex2}");
            }
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Menu = new RefresherMenuBar();
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
        
        State.Logger.LogDebug(OSIntegration, $"Showing child form {form.GetType().Name} '{form.Title}'");

        if (close)
        {
            Application.Instance.MainForm = form;
            this.Close();
        }
    }

    protected void UpdateSubtitle(string subtitle)
    {
        this.Title = "Refresher";
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            this.Title += " - " + subtitle;
        }
    }

    private class RefresherMenuBar : MenuBar
    {
        public RefresherMenuBar() {
            this.Style = "MenuBar";
            this.IncludeSystemItems = MenuBarSystemItems.All;
        }
    }
}