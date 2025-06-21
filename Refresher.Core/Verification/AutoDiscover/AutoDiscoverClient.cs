using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Refresher.Core.Platform;

namespace Refresher.Core.Verification.AutoDiscover;

public static class AutoDiscoverClient
{
    public static async Task<AutoDiscoverResponse?> InvokeAutoDiscoverAsync(string url, IPlatformInterface platform, CancellationToken cancellationToken = default)
    {
        url = TryCompleteUrl(url);
        
        State.Logger.LogInfo(LogType.AutoDiscover, $"Invoking autodiscover on URL '{url}'");
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? autodiscoverUri))
        {
            platform.ErrorPrompt("Server URL could not be parsed correctly. AutoDiscover cannot continue.");
            return null;
        }
        
        Debug.Assert(autodiscoverUri != null);
        
        if (autodiscoverUri.Host.Contains("lnfinite", StringComparison.InvariantCultureIgnoreCase) ||
            autodiscoverUri.Host.Contains("infinite", StringComparison.InvariantCultureIgnoreCase))
        {
            platform.WarnPrompt("The operators of this server are known to spread misinformation," +
                                " run outdated software or fall behind on security updates," +
                                " and generally don't play nice with other LBP communities.\r\n" + 
                                "It is highly advised you pick another server to play on. There's plenty out there.\r\n\r\n" +
                                "Refresher will allow you to play on this instance, but please be cautious.");

            QuestionResult result = platform.Ask("Are you sure you still want to play on this instance?");
            if (result == QuestionResult.No)
            {
                platform.InfoPrompt("Got it. As an alternative, you can try:\r\n\r\n" +
                                    "Bonsai: https://lbp.lbpbonsai.com\r\n" +
                                    "Beacon: https://beacon.lbpunion.com\r\n");
                return null;
            }
        }
        
        try
        {
            using HttpClient client = new();
            client.BaseAddress = autodiscoverUri;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Refresher/" + AutoDiscoverResponse.SupportedVersion);
            
            HttpResponseMessage response = await client.GetAsync("/autodiscover", cancellationToken);
            response.EnsureSuccessStatusCode();

            AutoDiscoverResponse? autodiscover = await response.Content.ReadFromJsonAsync<AutoDiscoverResponse>(cancellationToken: cancellationToken);
            if (autodiscover == null) throw new InvalidOperationException("autoresponse was null");
            
            if(autodiscover.Version != AutoDiscoverResponse.SupportedVersion)
                platform.WarnPrompt(
                    $"The server gave a response with version v{autodiscover.Version}, but we expected v{AutoDiscoverResponse.SupportedVersion}. " +
                    $"We will accept this response, but if things go wrong, try updating Refresher.");
            
            string text = $"Successfully found a '{autodiscover.ServerBrand}' server at the given URL!\r\n" +
                          $"Server Description: {autodiscover.ServerDescription}\r\n" +
                          $"Server's recommended patch URL: {autodiscover.Url}\r\n" +
                          $"Banner URL: {autodiscover.BannerImageUrl}\r\n" +
                          $"Custom digest key?: {(autodiscover.UsesCustomDigestKey.GetValueOrDefault() ? "Yes" : "No")}";
            
            State.Logger.LogInfo(LogType.AutoDiscover, text);

            return autodiscover;
        }
        catch (AggregateException aggregate)
        {
            aggregate.Handle(inner => HandleAutoDiscoverError(platform, inner));
        }
        catch(Exception e)
        {
            if (!HandleAutoDiscoverError(platform, e))
            {
                SentrySdk.CaptureException(e);
                platform.ErrorPrompt($"AutoDiscover failed for an unknown reason: {e}");
            }
        }

        return null;
    }
    
    private static bool HandleAutoDiscoverError(IPlatformInterface platform, Exception inner)
    {
        if (inner is HttpRequestException httpException)
        {
            if (httpException.StatusCode == null)
            {
                platform.ErrorPrompt($"AutoDiscover failed, because we couldn't communicate with the server: {inner.Message}");
                return true;
            }

            if (httpException.StatusCode == HttpStatusCode.NotFound)
            {
                platform.ErrorPrompt("AutoDiscover failed, because the server likely doesn't support it. " +
                                     "If you're patching an LBP game, this generally means the server you're trying to connect to is outdated.");
                return true;
            }
            
            platform.ErrorPrompt($"AutoDiscover failed, because the server responded with {(int)httpException.StatusCode} {httpException.StatusCode}.");
            return true;
        }
        
        if (inner is SocketException)
        {
            platform.ErrorPrompt($"AutoDiscover failed, because we couldn't communicate with the server: {inner.Message}");
            return true;
        }

        if (inner is JsonException)
        {
            platform.ErrorPrompt("AutoDiscover failed, because the server sent invalid data. There might be an outage; please try again in a few moments.");
            return true;
        }

        if (inner is NotSupportedException)
        {
            platform.ErrorPrompt($"AutoDiscover failed due to something we couldn't support: {inner.Message}");
            return true;
        }

        if (inner is OperationCanceledException)
        {
            State.Logger.LogWarning(LogType.AutoDiscover, "AutoDiscover was cancelled by the user.");
            return true;
        }
        
        return false;
    }

    private static string TryCompleteUrl(string url)
    {
        if (url.StartsWith("http")) return url;

        // cool GAMER shortcuts
        switch (url.ToLowerInvariant())
        {
            case "r":
            case "refresh":
            case "bo":
            case "bonsai":
                return "https://lbp.littlebigrefresh.com";
            case "l":
            case "local":
                return "http://localhost:10061";
            case "b":
            case "beacon":
            case "lighthouse":
                // technically, beacon doesn't support autodiscover so this is more future-proofing if they ever do for some reason
                return "https://beacon.lbpunion.com";
            case "refreshed":
                State.Logger.LogError("jvyden", "you're gonna make me cry. it's refresh. not refreshed.");
                goto case "refresh";
            default:
                // prefer HTTPS by default if there's no scheme set.
                return "https://" + url;
        }
    }
}