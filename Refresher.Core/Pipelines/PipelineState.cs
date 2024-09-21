namespace Refresher.Core.Pipelines;

public enum PipelineState : byte
{
    NotStarted,
    Running,
    Finished,
    Cancelled,
    Error,
}