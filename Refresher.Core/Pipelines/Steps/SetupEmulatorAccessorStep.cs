using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class SetupEmulatorAccessorStep : Step
{
    public SetupEmulatorAccessorStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }

    public override List<StepInput> Inputs { get; } = [
        new("hdd0-path", "RPCS3 dev_hdd0 folder", StepInputType.Directory)
        {
            // provide an example to Windows users.
            // don't bother with other platforms because they should be automatic
            Placeholder = @$"C:\Users\{Environment.UserName}\RPCS3\dev_hdd0",
            DetermineDefaultValue = DetermineDefaultPath,
        },
    ];

    // TODO: Cache the last used location for easier entry
    private static string? DetermineDefaultPath()
    {
        // RPCS3 builds for Windows are portable, so we can't determine this automatically
        if (OperatingSystem.IsWindows())
            return null;

        // ~/.config/rpcs3/dev_hdd0
        string folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rpcs3", "dev_hdd0");

        if (Directory.Exists(folder))
            return folder;
        
        return null;
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string path = this.Inputs[0].GetValueFromPipeline(this.Pipeline);
        State.Logger.LogDebug(RPCS3, $"Using RPCS3 path {path}");
        this.Pipeline.Accessor = new EmulatorPatchAccessor(path);

        return Task.CompletedTask;
    }
}