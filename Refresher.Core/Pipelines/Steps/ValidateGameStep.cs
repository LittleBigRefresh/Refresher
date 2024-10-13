using Refresher.Core.Patching;

namespace Refresher.Core.Pipelines.Steps;

public class ValidateGameStep : Step
{
    public ValidateGameStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }

    public override List<StepInput> Inputs { get; } =
    [
        CommonStepInputs.TitleId,
    ];

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string titleId = CommonStepInputs.TitleId.GetValueFromPipeline(this.Pipeline).Trim();
        string gamePath = $"game/{titleId}";

        // sanity check. UI will not allow this to happen, but CLI will
        if (titleId.Length != "NPUA80662".Length)
            throw new InvalidOperationException("Title ID does not match expected length. Did you type the ID in correctly?");
        
        if(!this.Pipeline.Accessor!.DirectoryExists(gamePath))
            throw new FileNotFoundException("The game directory does not exist. This usually means you haven't installed any updates for your game.");
        
        this.Pipeline.GameInformation = new GameInformation
        {
            TitleId = titleId,
        };

        return Task.CompletedTask;
    }
}