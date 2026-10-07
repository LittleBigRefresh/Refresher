using Refresher.Core.Patching.Steps.Legacy;

namespace Refresher.Core.Patching.Pipelines.Legacy;

public class ElfToElfPatchPipeline : Pipeline
{
    public override string Name => ".elf->.elf Patch";
    
    protected override List<Type> StepTypes =>
    [
        typeof(InputElfStep),

        typeof(PrepareEbootPatcherAndVerifyStep),
        typeof(ApplyPatchToEbootStep),
        
        typeof(OutputElfStep),
    ];
}