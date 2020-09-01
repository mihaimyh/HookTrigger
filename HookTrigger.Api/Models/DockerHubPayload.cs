namespace HookTrigger.Api.Models
{
    public class DockerHubPayload
    {
        public string CallbackUrl { get; set; }
        public PushData PushData { get; set; }
        public Repository Repository { get; set; }
    }

    public class PushData
    {
        public string[] Images { get; set; }
        public float PushedAt { get; set; }
        public string Pusher { get; set; }
        public string Tag { get; set; }
    }

    public class Repository
    {
        public int CommentCount { get; set; }
        public float DateCreated { get; set; }
        public string Description { get; set; }
        public string Dockerfile { get; set; }
        public string FullDescription { get; set; }
        public bool IsOfficial { get; set; }
        public bool IsP5rivate { get; set; }
        public bool IsTrusted { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Owner { get; set; }
        public string RepoName { get; set; }
        public string RepoUrl { get; set; }
        public int StarCount { get; set; }
        public string Status { get; set; }
    }
}