using Microsoft.Extensions.Options;
using System.Text;
using WebApiDemo.Models;

public interface ISpeechService
{
    Task<byte[]> GenerateSpeechAsync(
        SpeechRequest request,
        CancellationToken cancellationToken);
}

public sealed class AzureSpeechService : ISpeechService
{
    private readonly HttpClient _httpClient;
    private readonly AzureSpeechOptions _options;

    public AzureSpeechService(
        HttpClient httpClient,
        IOptions<AzureSpeechOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<byte[]> GenerateSpeechAsync(
        SpeechRequest request,
        CancellationToken cancellationToken)
    {
        var voice = request.Voice ?? ResolveVoice(request.Language);

        var ssml = BuildSsml(
            request.Text,
            voice,
            request.Language);

        var endpoint = BuildEndpoint();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        httpRequest.Headers.Add(
            "Ocp-Apim-Subscription-Key",
            _options.SubscriptionKey);

        httpRequest.Headers.Add(
            "X-Microsoft-OutputFormat",
            _options.OutputFormat);

        httpRequest.Headers.Add(
            "User-Agent",
            "WebApiDemo.TTS");

        httpRequest.Content = new StringContent(
            ssml,
            Encoding.UTF8,
            "application/ssml+xml");

        var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        var bytes = await response.Content
            .ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = Encoding.UTF8.GetString(bytes);
            throw new ApplicationException($"TTS failed: {error}");
        }

        return bytes;
    }

    // 🔥 FIX: Proper endpoint construction
    private string BuildEndpoint()
    {
        return $"{_options.BaseUrl.TrimEnd('/')}/cognitiveservices/v1";
    }

    private static string BuildSsml(
        string text,
        string voice,
        string language)
    {
        return $"""
<speak version="1.0"
       xmlns="http://www.w3.org/2001/10/synthesis"
       xml:lang="{language}">
    <voice name="{voice}">
        <prosody rate="0.95" pitch="0%">
            {System.Security.SecurityElement.Escape(text)}
        </prosody>
    </voice>
</speak>
""";
    }

    private static string ResolveVoice(string language)
    {
        return language switch
        {
            "te-IN" => "te-IN-ShrutiNeural",
            "hi-IN" => "hi-IN-SwaraNeural",
            "en-IN" => "en-IN-NeerjaNeural",
            _ => "en-IN-NeerjaNeural"
        };
    }
}