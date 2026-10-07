namespace Refresher.Core.Patching.Pipelines;

public enum PipelineState : byte
{
    NotStarted,
    Running,
    Finished,
    Cancelled,
    Error,
}