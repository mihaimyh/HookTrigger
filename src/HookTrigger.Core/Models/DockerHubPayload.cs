using System.Text.Json.Serialization;

namespace HookTrigger.Core.Models
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