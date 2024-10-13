using Refresher.Core.Accessors;
using Refresher.Core.Patching;
using Refresher.Core.Verification;

namespace Refresher.Core.Pipelines.Steps;

public class DownloadParamSfoStep : Step
{
    public DownloadParamSfoStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GameInformation game = this.Pipeline.GameInformation!;
        string gamePath = $"game/{game.TitleId}";

        Stream? sfoStream = null;
        PatchAccessor.Try(() =>
        {
            string sfoLocation = $"{gamePath}/PARAM.SFO";

            if(this.Pipeline.Accessor!.FileExists(sfoLocation)) 
                sfoStream = this.Pipeline.Accessor.OpenRead(sfoLocation);
        });

        ParamSfo? sfo = null;
        try
        {
            if (sfoStream == null)
            {
                throw new FileNotFoundException("The PARAM.SFO file does not exist. This usually means you haven't installed any updates for your game.");
            }
            this.ParseSfoStream(sfoStream, out sfo);
        }
        catch (EndOfStreamException)
        {
            State.Logger.LogError(InfoRetrieval, $"Couldn't load {game}'s PARAM.SFO because the file was incomplete.");
        }
        catch(Exception e)
        {
            game.Name = $"Unknown PARAM.SFO [{game}]";
                
            State.Logger.LogError(InfoRetrieval, $"Couldn't load {game}'s PARAM.SFO: {e}");
            if (sfo != null)
            {
                State.Logger.LogDebug(InfoRetrieval, $"PARAM.SFO version:{sfo.Version} dump:");
                foreach ((string? key, object? value) in sfo.Table)
                {
                    State.Logger.LogDebug(InfoRetrieval, $"  '{key}' = '{value}'");
                }
            }
            else
            {
                State.Logger.LogWarning(InfoRetrieval, "PARAM.SFO was not read, can't dump");
            }
                
            SentrySdk.CaptureException(e);
        }

        State.Logger.LogInfo(InfoRetrieval, "Parsed PARAM.SFO: " + game);
        return Task.CompletedTask;
    }

    private void ParseSfoStream(Stream sfoStream, out ParamSfo sfo)
    {
        sfo = new ParamSfo(sfoStream);
        GameInformation info = this.Pipeline.GameInformation!;

        info.Version = "01.00";
        if (sfo.Table.TryGetValue("APP_VER", out object? value))
        {
            string? appVersion = value.ToString();
            if (appVersion != null)
                info.Version = appVersion;
        }

        info.Name = sfo.Table["TITLE"].ToString();
    }
}