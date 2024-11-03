using System.Diagnostics;
using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class GetConsoleIdpsStep : Step
{
    public GetConsoleIdpsStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        ConsolePatchAccessor? accessor = this.Pipeline.Accessor as ConsolePatchAccessor;
        Debug.Assert(accessor != null);

        if (this.Game.ShouldUseNpdrmEncryption.GetValueOrDefault())
        {
            this.Encryption.ConsoleIdps = accessor.IdpsFile.Value;
            // ^ is lazy<t>, will generate upon use
        }

        return Task.CompletedTask;
    }
}