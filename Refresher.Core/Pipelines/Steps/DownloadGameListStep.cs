using Refresher.Core.Accessors;
using Refresher.Core.Patching;

namespace Refresher.Core.Pipelines.Steps;

public class DownloadGameListStep : Step
{
    public DownloadGameListStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        this.Pipeline.GameList = [];

        List<string> games = this.Pipeline.Accessor!.GetDirectoriesInDirectory("game").ToList();
        int i = 0;
        foreach (string gamePath in games)
        {
            State.Logger.LogInfo(InfoRetrieval, $"Downloading information for game '{gamePath}'...");
            if (!this.Pipeline.Accessor.FileExists(Path.Combine(gamePath, "PARAM.SFO")))
            {
                State.Logger.LogDebug(InfoRetrieval, $"{gamePath} has no PARAM.SFO, skipping...");
                continue;
            }
            
            GameInformation game = new()
            {
                TitleId = Path.GetFileName(gamePath),
            };
            this.Pipeline.GameInformation = game;
            
            DownloadParamSfoStep paramStep = new(this.Pipeline);
            await paramStep.ExecuteAsync(cancellationToken);
            
            DownloadIconStep iconStep = new(this.Pipeline);
            await iconStep.ExecuteAsync(cancellationToken);
            
            this.Pipeline.GameList.Add(game);
            this.Progress = i++ / (float)games.Count;
        }

        this.Pipeline.GameInformation = null;
    }
}