namespace Refresher.Core.Pipelines;

internal class CommonStepInputs
{
    internal static readonly StepInput TitleId = new("title-id", "Game to patch", StepInputType.Game)
    {
        Placeholder = "NPUA80662",
    };
}