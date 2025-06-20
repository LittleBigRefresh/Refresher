using System.Reflection;
using Refresher.Core.Accessors;
using Refresher.Core.Platform;

namespace Refresher.Core.Pipelines.Steps;

public class UploadPatchworkSprxStep : Step
{
    public UploadPatchworkSprxStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await PatchAccessor.TryAsync(async () =>
        {
            const string pluginsFolder = "plugins/";
            const string sprxName = "patchwork.sprx";
            const string sprxNameEmulator = "patchwork-rpcs3.sprx";
            const string sprxPath = pluginsFolder + sprxName;

            this.Pipeline.Accessor!.CreateDirectoryIfNotExists(pluginsFolder);

            if (this.Pipeline.Accessor.FileExists(sprxPath))
                this.Pipeline.Accessor.RemoveFile(sprxPath);
            
            this.Progress = 0.5f;
            
            string localSprxName = this.Pipeline.Accessor is EmulatorPatchAccessor
                ? sprxNameEmulator
                : sprxName;

            const string question = "Found custom patchwork.sprx next to exe file, upload that instead?";

            if (File.Exists(localSprxName) && this.Platform.Ask(question) == QuestionResult.Yes)
            {
                this.Pipeline.Accessor.UploadFile(localSprxName, sprxPath);
            }
            else
            {
                await using Stream writeStream = this.Pipeline.Accessor.OpenWrite(sprxPath);
                await using Stream? readStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(localSprxName);

                if (readStream == null)
                {
                    await this.Fail($"The sprx file for {this.Pipeline.Accessor.GetType().Name} is missing from this build! " +
                                    $"Please tell a developer on Discord/GitHub!");

                    return;
                }

                await readStream.CopyToAsync(writeStream, cancellationToken);
                await writeStream.FlushAsync(cancellationToken);
            }
        });
    }
}