using Refresher.Core.Patching;

namespace Refresher.Core.Pipelines.Steps;

public class InputElfStep : Step
{
    public InputElfStep(Pipeline pipeline) : base(pipeline)
    {}

    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.ElfInput,
    ];

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string elfInput = this.Inputs.First().GetValueFromPipeline(this.Pipeline);
        if (!File.Exists(elfInput))
            return this.Fail("The Input .ELF could not be found.");

        string temp = Path.GetTempFileName();

        {
            using FileStream write = File.OpenWrite(temp);
            using FileStream read = File.OpenRead(elfInput);
            read.CopyTo(write);
        }

        this.Pipeline.GameInformation = new GameInformation
        {
            DecryptedEbootPath = temp,
            Name = Path.GetFileName(elfInput),
            TitleId = "UNKN12345",
        };

        return Task.CompletedTask;
    }
}