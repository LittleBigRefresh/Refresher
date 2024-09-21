namespace Refresher.Core.Pipelines;

public class StepInput
{
    public string Id { get; init; }
    public string Name { get; init; }

    public StepInput(string id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public string GetValueFromPipeline(Pipeline pipeline)
    {
        bool success = pipeline.Inputs.TryGetValue(this.Id, out string? input);
        if(!success)
            throw new Exception($"Input {this.Id} was not provided to the pipeline before execution.");

        return input!;
    }
}