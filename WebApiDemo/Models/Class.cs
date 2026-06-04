namespace WebApiDemo.Models
{

    public class AzureSpeechOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string SubscriptionKey { get; set; } = string.Empty;
        public string OutputFormat { get; set; } = "audio-16khz-32kbitrate-mono-mp3";
    }

    public sealed class ChatResponse
    {
        public string ResponseId { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
    public sealed class ChatRequest
    {
        public string Message { get; set; } = string.Empty;

        public string? PreviousResponseId { get; set; }
    }


    public sealed class SpeechRequest
    {
        public string Text { get; set; } = string.Empty;

        public string Language { get; set; } = "en-IN";

        public string? Voice { get; set; }
    }
}
