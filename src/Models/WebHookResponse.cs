using Newtonsoft.Json;

namespace discord_web_hook_logger.Models
{
    internal class WebHookResponse
    {
        public WebHookResponse(string returnValue, DiscordRateLimitResponse rateLimitResponse = null)
        {
            ReturnValue = returnValue;
            RateLimitResponse = rateLimitResponse;
        }

        public string ReturnValue { get; }
        public DiscordRateLimitResponse RateLimitResponse { get; }
    }

    internal class DiscordRateLimitResponse
    {
        [JsonProperty("message")]
        public string Message { get; private set; }

        [JsonProperty("retry_after")]
        public int RetryAfter { get; private set; }

        [JsonProperty("global")]
        public bool GlobalRateLimit { get; private set; }
    }
}