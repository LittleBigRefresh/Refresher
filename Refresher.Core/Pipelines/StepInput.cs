namespace Refresher.Core.Pipelines;

public class StepInput
{
    public string Id { get; init; }
    public string Name { get; init; }
    public StepInputType Type { get; init; }

    public string Placeholder { get; init; }
    public bool Required { get; init; }

    public Func<string?>? DetermineDefaultValue { get; init; }
    public bool ShouldCauseGameDownloadWhenChanged { get; init; }

    public StepInput(string id, string name, StepInputType type = StepInputType.Text)
    {
        this.Id = id;
        this.Name = name;
        this.Type = type;
    }

    public string GetValueFromPipeline(Pipeline pipeline)
    {
        bool success = pipeline.Inputs.TryGetValue(this.Id, out string? input);
        if(!success)
            throw new Exception($"Input {this.Id} was not provided to the pipeline before execution.");

        return input!;
    }
}