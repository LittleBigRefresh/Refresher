using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

namespace Refresher.Core.Verification.AutoDiscover;

public static class AutoDiscoverClient
{
    public static async Task<AutoDiscoverResponse?> InvokeAutoDiscoverAsync(string url, CancellationToken cancellationToken = default)
    {
        if(!url.StartsWith("http"))
            url = "https://" + url; // prefer HTTPS by default if there's no scheme set.
        
        State.Logger.LogInfo(LogType.AutoDiscover, $"Invoking autodiscover on URL '{url}'");
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? autodiscoverUri))
        {
            State.Logger.LogError(LogType.AutoDiscover, "Server URL could not be parsed correctly. AutoDiscover cannot continue.");
            return null;
        }
        
        Debug.Assert(autodiscoverUri != null);
        try
        {
            using HttpClient client = new();
            client.BaseAddress = autodiscoverUri;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Refresher/3");
            
            HttpResponseMessage response = await client.GetAsync("/autodiscover", cancellationToken);
            response.EnsureSuccessStatusCode();

            AutoDiscoverResponse? autodiscover = await response.Content.ReadFromJsonAsync<AutoDiscoverResponse>(cancellationToken: cancellationToken);
            if (autodiscover == null) throw new InvalidOperationException("autoresponse was null");
            
            string text = $"Successfully found a '{autodiscover.ServerBrand}' server at the given URL!\n" +
                          $"Server's recommended patch URL: {autodiscover.Url}\n" +
                          $"Custom digest key?: {(autodiscover.UsesCustomDigestKey.GetValueOrDefault() ? "Yes" : "No")}";
            
            State.Logger.LogInfo(LogType.AutoDiscover, text);

            return autodiscover;
        }
        catch (AggregateException aggregate)
        {
            aggregate.Handle(HandleAutoDiscoverError);
        }
        catch(Exception e)
        {
            if (!HandleAutoDiscoverError(e))
            {
                SentrySdk.CaptureException(e);
                State.Logger.LogError(LogType.AutoDiscover, $"AutoDiscover failed for an unknown reason: {e}");
            }
        }

        return null;
    }
    
    private static bool HandleAutoDiscoverError(Exception inner)
    {
        if (inner is HttpRequestException httpException)
        {
            if (httpException.StatusCode == null)
            {
                State.Logger.LogError(LogType.AutoDiscover, $"AutoDiscover failed, because we couldn't communicate with the server: {inner.Message}");
                return true;
            }
            
            State.Logger.LogError(LogType.AutoDiscover, $"AutoDiscover failed, because the server responded with {(int)httpException.StatusCode} {httpException.StatusCode}.");
            return true;
        }
        
        if (inner is SocketException)
        {
            State.Logger.LogError(LogType.AutoDiscover, $"AutoDiscover failed, because we couldn't communicate with the server: {inner.Message}");
            return true;
        }

        if (inner is JsonException)
        {
            State.Logger.LogError(LogType.AutoDiscover, "AutoDiscover failed, because the server sent invalid data. There might be an outage; please try again in a few moments.");
            return true;
        }

        if (inner is NotSupportedException)
        {
            State.Logger.LogError(LogType.AutoDiscover, $"AutoDiscover failed due to something we couldn't support: {inner.Message}");
            return true;
        }

        if (inner is TaskCanceledException)
        {
            State.Logger.LogWarning(LogType.AutoDiscover, "AutoDiscover was cancelled by the user.");
            return true;
        }
        
        return false;
    }
}