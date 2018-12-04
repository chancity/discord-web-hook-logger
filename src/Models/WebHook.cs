using System.Collections.Generic;
using Newtonsoft.Json;

namespace discord_web_hook_logger.Models
{
    public class WebHook
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};

        [JsonIgnore]
        internal string WebHookUrl { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonIgnore]
        public string FileData { get; set; }

        [JsonProperty("tts")]
        public bool IsTTS { get; set; }

        [JsonProperty("embeds")]
        public List<Embed> Embeds { get; set; } = new List<Embed>();

        [JsonIgnore]
        public string PayloadJson => JsonConvert.SerializeObject(this, Formatting.None, JsonSerializerSettings);
    }
}