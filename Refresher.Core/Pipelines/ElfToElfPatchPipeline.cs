using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines;

public class ElfToElfPatchPipeline : Pipeline
{
    public override string Id => "elf-elf-patch";
    public override string Name => ".elf->.elf Patch";
    
    protected override List<Type> StepTypes =>
    [
        typeof(InputElfStep),

        typeof(PrepareEbootPatcherAndVerifyStep),
        typeof(ApplyPatchToEbootStep),
        
        typeof(OutputElfStep),
    ];
}