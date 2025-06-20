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

        List<string> games = this.Pipeline.Accessor!.GetDirectoriesInDirectory("game")
            .Where(p => Path.GetFileName(p).Length == "NPUA80662".Length)
            .ToList();

        bool isConsole = this.Pipeline.Accessor is ConsolePatchAccessor;

        int i = 0;
        foreach (string gamePath in games)
        {
            State.Logger.LogInfo(InfoRetrieval, $"Downloading information for game '{gamePath}'...");
            
            GameInformation game = new()
            {
                TitleId = Path.GetFileName(gamePath),
            };
            this.Pipeline.GameInformation = game;

            const int maxTries = 5;
            int tries = maxTries + 1;
            while (--tries != 0)
            {
                try
                {
                    DownloadParamSfoStep paramStep = new(this.Pipeline);
                    await paramStep.ExecuteAsync(cancellationToken);

                    DownloadIconStep iconStep = new(this.Pipeline);
                    await iconStep.ExecuteAsync(cancellationToken);

                    break;
                }
                catch(Exception e)
                {
                    State.Logger.LogWarning(InfoRetrieval, $"Failed to get information for '{game.TitleId}'. {tries} tries remaining...");
                    State.Logger.LogWarning(InfoRetrieval, e.ToString());
                    if (isConsole)
                        await Task.Delay(5000, cancellationToken);
                }
            }

            if (tries == 0)
            {
                this.Platform.WarnPrompt($"Couldn't get information for '{game.TitleId}' after {maxTries} tries. Giving up.");
            }

            if (isConsole)
                await Task.Delay(100, cancellationToken);
            
            this.Progress = i++ / (float)games.Count;

            List<string> filters = this.Pipeline.GameNameFilters.ToList();
            if(filters.Count <= 0 || filters.Any(f => game.Name?.Contains(f, StringComparison.InvariantCultureIgnoreCase) ?? false))
                this.Pipeline.GameList.Add(game);
        }

        this.Pipeline.GameInformation = null;
    }
}