using Refresher.Core.Patching;
using SCEToolSharp;

namespace Refresher.Core.Pipelines.Steps;

public class DownloadGameLicenseStep : Step
{
    public DownloadGameLicenseStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GameInformation game = this.Game;
        string contentId = game.ContentId!;
        
        string licenseDir = Path.Join(Path.GetTempPath(), "refresher-" + Random.Shared.Next());
        Directory.CreateDirectory(licenseDir);

        this.Pipeline.EncryptionDetails = new EncryptionDetails()
        {
            LicenseDirectory = licenseDir,
        };
        
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
                //If the license file does not contain the content ID in its path, skip it
                if (!licenseFile.Contains(contentId) && !licenseFile.Contains(game.TitleId))
                    continue;
                
                State.Logger.LogDebug(Crypto, $"Found compatible rap: {licenseFile}");

                string actDatPath = Path.Combine(user, "exdata", "act.dat");
                    
                //If it is a valid content id, lets download that user's act.dat, if its there
                if (!found && this.Pipeline.Accessor.FileExists(actDatPath))
                {
                    string downloadedActDat = this.Pipeline.Accessor.DownloadFile(actDatPath);
                    this.Encryption.DownloadedActDatPath = downloadedActDat;
                }

                //And the license file
                string downloadedLicenseFile = this.Pipeline.Accessor.DownloadFile(licenseFile);
                File.Move(downloadedLicenseFile, Path.Join(licenseDir, Path.GetFileName(licenseFile)));

                State.Logger.LogInfo(Crypto, $"Downloaded compatible license file {licenseFile}.");

                if(Path.GetFileNameWithoutExtension(licenseFile) == game.ContentId) 
                    found = true;
            }

            if (found) 
                break;
        }

        if (!found)
        {
            this.Game.ShouldUseNpdrmEncryption = false;
            State.Logger.LogWarning(Crypto, "Couldn't find a license file for {0}. For disc copies, this is normal." +
                                            "For digital copies, this may present problems. Attempting to continue without it...", game.TitleId);
        }
        else
        {
            this.Game.ShouldUseNpdrmEncryption = true;
        }
        
        return Task.CompletedTask;
    }
}