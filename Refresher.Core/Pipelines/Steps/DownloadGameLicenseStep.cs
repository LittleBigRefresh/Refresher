using Refresher.Core.Patching;

namespace Refresher.Core.Pipelines.Steps;

public class DownloadGameLicenseStep : Step
{
    public DownloadGameLicenseStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GameInformation game = this.Game;
        string? contentId = game.ContentId;

        if (contentId == null)
            throw new NotImplementedException("Cannot pick a license to decrypt with without a content id");
        
        bool found = false;
        foreach (string user in this.Pipeline.Accessor!.GetDirectoriesInDirectory(Path.Combine("home")))
        {
            State.Logger.LogDebug(Crypto, $"Checking all license files in {user}");
            string exdataFolder = Path.Combine(user, "exdata");

            if (!this.Pipeline.Accessor.DirectoryExists(exdataFolder))
            {
                State.Logger.LogDebug(Crypto, $"Exdata folder doesn't exist for user {user}, skipping...");
                continue;
            }
            
            foreach (string licenseFile in this.Pipeline.Accessor.GetFilesInDirectory(exdataFolder))
            {
                // If the license file does not start with the content ID, skip it.
                if (!Path.GetFileName(licenseFile).StartsWith(contentId))
                    continue;

                string actDatPath = Path.Combine(user, "exdata", "act.dat");
                    
                //If it is a valid content id, lets download that user's act.dat, if its there
                if (!found && this.Pipeline.Accessor.FileExists(actDatPath))
                {
                    string downloadedActDat = this.Pipeline.Accessor.DownloadFile(actDatPath);
                    this.Encryption.DownloadedActDatPath = downloadedActDat;
                }

                //And the license file
                string downloadedLicenseFile = this.Pipeline.Accessor.DownloadFile(licenseFile);
                this.Encryption.DownloadedLicensePath = downloadedLicenseFile;

                State.Logger.LogInfo(Crypto, $"Downloaded license file '{licenseFile}'.");
                found = true;
                break;
            }

            if (found) 
                break;
        }

        if (!found && this.Game.ShouldUseNpdrmEncryption.GetValueOrDefault())
        {
            State.Logger.LogWarning(Crypto, "Couldn't find a license file for {0}. " +
                                            "This may present problems. Attempting to continue without it...", game.TitleId);
        }
        
        return Task.CompletedTask;
    }
}