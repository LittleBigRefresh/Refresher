using Eto.Drawing;
using Eto.Forms;
using Refresher.Core.Pipelines;
using Refresher.Core.Pipelines.Lbp;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace Refresher.UI;

/// <summary>
/// Presents a list of patchers that the user can use to patch for their platform.
/// </summary>
public class MainForm : RefresherForm
{
    private readonly Label _updateStatusLabel;

    private readonly UpdateManager _velo = new(new GithubSource("https://github.com/LittleBigRefresh/Refresher", null, false));
    private UpdateInfo? _updateInfo;

    private string UpdateStatus
    {
        get => this._updateStatusLabel.Text;
        set
        {
            Application.Instance.Invoke(() =>
            {
                this._updateStatusLabel.Text = value;
            });
        }
    }

    public MainForm() : base(string.Empty, new Size(450, -1))
    {
        StackLayout layout;
        this.Content = layout = new StackLayout
        // ReSharper disable once RedundantExplicitParamsArrayCreation
        ([
            new Label { Text = "Welcome to Refresher! Please pick a patching method to continue." },
            this._updateStatusLabel = new Label(),
            new Label { Text = "LittleBigPlanet:" },
            this.PipelineButton<LbpPS3PatchPipeline>("Patch LBP1/2/3 for PS3"),
            this.PipelineButton<PatchworkPS3ConfigPipeline>("Reconfigure Patch for PS3"),
            this.PipelineButton<LbpRPCS3PatchPipeline>("Patch LBP1/2/3 for RPCS3"),
            this.PipelineButton<PatchworkRPCS3ConfigPipeline>("Reconfigure Patch for RPCS3"),

            new Label { Text = "General (for non-LBP games):" },
            this.PipelineButton<RPCS3PatchPipeline>("Patch any RPCS3 game"),
            this.PipelineButton<PS3PatchPipeline>("Patch any PS3 game"),

            new Label { Text = "Advanced (for experts):" },
            this.PipelineButton<ElfToElfPatchPipeline>(".elf->.elf Patch"),

            #if DEBUG
            new Label { Text = "Debugging options:" },
            this.PipelineButton<ExamplePipeline>("Example Pipeline"),
            #endif
        ]);

        layout.Spacing = 5;
        layout.HorizontalContentAlignment = HorizontalAlignment.Stretch;

        this._updateStatusLabel.Text = "Loading updater...";
    }

    protected override void OnLoadComplete(EventArgs e)
    {
        base.OnLoadComplete(e);
        this.UpdateStatus = "Checking for updates...";

        Task.Run(async () =>
        {
            try
            {
                await this.CheckForUpdatesAsync();
            }
            catch (NotInstalledException)
            {
                this.UpdateStatus = "This build of Refresher cannot be updated.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check for updates: {ex}", "Refresher", MessageBoxType.Error);
            }
        });
    }

    private async Task CheckForUpdatesAsync()
    {
        UpdateInfo? newVersion = await this._velo.CheckForUpdatesAsync();
        if (newVersion == null)
        {
            this.UpdateStatus = $"Refresher is up to date! You are on v{this._velo.CurrentVersion?.ToNormalizedString() ?? "<unknown>"}.";
            return;
        }

        this.UpdateStatus = "Downloading update...";
        await this._velo.DownloadUpdatesAsync(newVersion);

        this._updateInfo = newVersion;
        
        this.UpdateStatus = $"An update is available. Click here to install Refresher {newVersion.TargetFullRelease.Version.ToNormalizedString()}!";
        await Application.Instance.InvokeAsync(() =>
        {
            this._updateStatusLabel.TextColor = Colors.Orange;
            this._updateStatusLabel.MouseUp += this.OnClickUpdate;
        });
    }

    private void OnClickUpdate(object? sender, MouseEventArgs e)
    {
        if (this._updateInfo == null)
            throw new InvalidOperationException("Cannot install an update if an update is not available");

        this.UpdateStatus = "Installing...";

        Task.Run(() =>
        {
            try
            {
                this._velo.ApplyUpdatesAndRestart(this._updateInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install updates: {ex}", "Refresher", MessageBoxType.Error);
            }
        });
    }

    private Button PipelineButton<TPipeline>(string name) where TPipeline : Pipeline, new()
    {
        return new Button((_, _) => this.ShowChild<PipelineForm<TPipeline>>()) { Text = name };
    }
}