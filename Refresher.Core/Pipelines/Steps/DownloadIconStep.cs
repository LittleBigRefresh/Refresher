using Refresher.Core.Accessors;
using Refresher.Core.Patching;
using Refresher.Core.Verification;

namespace Refresher.Core.Pipelines.Steps;

public class DownloadIconStep : Step
{
    public DownloadIconStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GameInformation game = this.Game;
        string gamePath = $"game/{game.TitleId}";
        
        string iconPath = Path.Combine(gamePath, "ICON0.PNG");
        if (!GameCacheAccessor.IconExistsInCache(game.TitleId) && this.Pipeline.Accessor!.FileExists(iconPath))
        {
            using Stream iconStream = this.Pipeline.Accessor.OpenRead(iconPath);
            GameCacheAccessor.WriteIconToCache(game.TitleId, iconStream);
        }

        return Task.CompletedTask;
    }
}