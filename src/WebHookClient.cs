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

        public bool CombineMessageTypes { get; set; } = false;

        public void SendLogMessage(string messageType, string message, Exception exception = null)
        {
            var toSend = new LogMessageItem
            {
                Message = message,
                MessageType = messageType,
                Color = GetMessageColor(messageType),
                StackTrace = exception?.StackTrace,
                Time = DateTime.UtcNow
            };


            SendLogMessage(toSend);
        }

        public void QueueLogMessage(string messageType, string message, Exception exception = null)
        {
            var toSend = new LogMessageItem
            {
                Message = message,
                MessageType = messageType,
                Color = GetMessageColor(messageType),
                StackTrace = exception?.StackTrace,
                Time = DateTime.UtcNow
            };

            LogMessageQueue.Enqueue(toSend);
        }

        private void SendLogMessage(LogMessageItem logMessageItem)
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
                FileData = logMessageItem.StackTrace
            };

            toSend.Embeds.Add(embed);

            SendMessage(toSend);
        }

        public void QueueMessage(WebHook toSend)
        {
            toSend.WebHookUrl = WebHookUrl;
            ToSendQueue.Enqueue(toSend);
        }

        public void SendMessage(WebHook toSend)
        {
            toSend.WebHookUrl = WebHookUrl;
            _Send(toSend).Wait();
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