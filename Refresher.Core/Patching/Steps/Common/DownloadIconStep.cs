using Refresher.Core.Patching;
using Refresher.Core.Patching.Pipelines;
using Refresher.Core.Storage;

namespace Refresher.Core.Patching.Steps.Common;

public class DownloadIconStep : Step
{
    public DownloadIconStep(Pipeline pipeline) : base(pipeline)
    {}

    public override string Name { get; } = "Downloading the icons of a game";
    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GameInformation game = this.Game;
        string gamePath = $"game/{game.TitleId}";
        
        string iconPath = Path.Combine(gamePath, "ICON0.PNG");
        if (!GameCacheStorage.IconExistsInCache(game.TitleId) && this.Pipeline.Accessor!.FileExists(iconPath))
        {
            using Stream iconStream = this.Pipeline.Accessor.OpenRead(iconPath);
            GameCacheStorage.WriteIconToCache(game.TitleId, iconStream);
        }

        return Task.CompletedTask;
    }
}