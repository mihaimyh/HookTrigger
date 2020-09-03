using System.Text.Json.Serialization;

namespace HookTrigger.Core.Models
{
    public class PushData
    {
        // comment to trigger github workflow
        public string[] Images { get; set; }

        [JsonPropertyName("pushed_at")]
        public float PushedAt { get; set; }

        public string Pusher { get; set; }

        public string Tag { get; set; }
    }
}