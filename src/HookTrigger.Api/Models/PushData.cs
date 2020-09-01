using System.Text.Json.Serialization;

namespace HookTrigger.Api.Models
{
    public class PushData
    {
        public string[] Images { get; set; }

        [JsonPropertyName("pushed_at")]
        public float PushedAt { get; set; }

        public string Pusher { get; set; }

        public string Tag { get; set; }
    }
}