using System;
using System.Collections.Generic;
using System.Drawing;
using discord_web_hook_logger.Extensions;
using discord_web_hook_logger.Models;
using Newtonsoft.Json;

namespace discord_web_hook_logger
{
    public partial class WebHookClient
    {
        private readonly Dictionary<string, int> _colorMap;

        public WebHookClient(long channelId, string channelToken, Dictionary<string, Color> colorMap = null) : this(
            $"https://discordapp.com/api/webhooks/{channelId}/{channelToken}", colorMap) { }

        public WebHookClient(string webhookUrl, Dictionary<string, Color> colorMap = null)
        {
            WebHookUrl = webhookUrl;
            _colorMap = new Dictionary<string, int>();

            MergeAllTypes = false;

            if (colorMap != null)
            {
                foreach (string key in colorMap.Keys)
                {
                    _colorMap.Add(key, colorMap[key].ToRgb());
                }
            }

            lock (LockObj)
            {
                WebHookClients.Add(this);
            }
        }

        [JsonIgnore]
        public string WebHookUrl { get; }

        public bool MergeAllTypes { get; set; }

        public void ForceSendLogMessage(string messageType, string message, string stackTrace = null)
        {
            var toSend = new LogMessageItem
            {
                Message = message,
                MessageType = messageType,
                Color = GetMessageColor(messageType),
                StackTrace = stackTrace,
                Time = DateTime.UtcNow
            };


            ForceSend(toSend);
        }

        public void QueueLogMessage(string messageType, string message, string stackTrace = null)
        {
            var toSend = new LogMessageItem
            {
                Message = message,
                MessageType = messageType,
                Color = GetMessageColor(messageType),
                StackTrace = stackTrace,
                Time = DateTime.UtcNow
            };


            LogMessageQueue.Enqueue(toSend);
        }

        private void ForceSend(LogMessageItem logMessageItem)
        {
            var embed = new Embed
            {
                Title = logMessageItem.MessageType,
                Color = logMessageItem.Color
            };

            embed.Fields.Add(new EmbedField
            {
                Inline = false,
                Name = $"{logMessageItem.Time}",
                Value = logMessageItem.Message
            });

            var toSend = new WebHook
            {
                WebHookUrl = WebHookUrl,
                FileData = logMessageItem.StackTrace
            };

            toSend.Embeds.Add(embed);
            _Send(toSend, toSend.FileData).Wait();
        }

        public void Send(WebHook toSend)
        {
            ToSendQueue.Enqueue(toSend);
        }

        private int GetMessageColor(string messageType)
        {
            if (!_colorMap.ContainsKey(messageType))
            {
                return DefaultColor;
            }

            return _colorMap[messageType];
        }

        protected bool Equals(WebHookClient other)
        {
            return string.Equals(WebHookUrl, other.WebHookUrl);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((WebHookClient) obj);
        }

        public override int GetHashCode()
        {
            return WebHookUrl != null ? WebHookUrl.GetHashCode() : 0;
        }
    }
}