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

    public class PushData
    {
        public string[] Images { get; set; }

        [JsonPropertyName("pushed_at")]
        public float PushedAt { get; set; }

        public string Pusher { get; set; }

        public string Tag { get; set; }
    }

    public class Repository
    {
        [JsonPropertyName("comment_count")]
        public int CommentCount { get; set; }

        [JsonPropertyName("date_created")]
        public float DateCreated { get; set; }

        public string Description { get; set; }

        public string Dockerfile { get; set; }

        [JsonPropertyName("full_description")]
        public string FullDescription { get; set; }

        [JsonPropertyName("is_official")]
        public bool IsOfficial { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsP5rivate { get; set; }

        [JsonPropertyName("is_trusted")]
        public bool IsTrusted { get; set; }

        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Owner { get; set; }

        [JsonPropertyName("repo_name")]
        public string RepoName { get; set; }

        [JsonPropertyName("repo_url")]
        public string RepoUrl { get; set; }

        [JsonPropertyName("star_count")]
        public int StarCount { get; set; }

        public string Status { get; set; }
    }
}