using System.Text.Json.Serialization;

namespace HookTrigger.Api.Models
{
    public class DockerHubPayload
    {
        [JsonPropertyName("callback_url")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("push_data")]
        public PushData PushData { get; set; }

        public Repository Repository { get; set; }
    }
}