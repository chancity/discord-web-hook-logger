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
        private readonly object _colorMapLockObject;
        private readonly object _mergeDiclockObject;
        private readonly Dictionary<string, Embed> _mergedMessagesDic;

        private WebHookClient(string webhookUrl)
        {
            _colorMapLockObject = new object();
            _mergeDiclockObject = new object();
            _mergedMessagesDic = new Dictionary<string, Embed>();
            WebHookUrl = webhookUrl;
            _colorMap = new Dictionary<string, int>();
        }

        private void UpdateColorMap(Dictionary<string, Color> colorMap)
        {
            if (colorMap != null)
            {
                lock (_colorMapLockObject)
                {
                    foreach (string key in colorMap.Keys)
                    {
                        _colorMap.Add(key, colorMap[key].ToRgb());
                    }
                }
            }
        }
        [JsonIgnore]
        public string WebHookUrl { get; }

        public bool CombineMessageTypes { get; set; } = false;

        public int PendingQueueSize
        {
            get
            {
                lock (_mergedMessagesDic)
                {
                    return _mergedMessagesDic.Count;
                }
            }
        }

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

        public void SendMessage(WebHook toSend)
        {
            toSend.WebHookUrl = WebHookUrl;
            _Send(toSend).Wait();
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

        public void QueueSendMessage(WebHook toSend)
        {
            toSend.WebHookUrl = WebHookUrl;
            ToSendQueue.Enqueue(toSend);
        }

        public void QueueLogMessage(string messageType, string message, Exception exception = null)
        {

            var logMessageItem = new LogMessageItem
            {
                Message = message,
                MessageType = messageType,
                Color = GetMessageColor(messageType),
                StackTrace = exception?.StackTrace,
                Time = DateTime.UtcNow
            };

            MergeLogMessage(logMessageItem);
        }

        private void MergedMessagesToSendQueue()
        {
            lock (_mergeDiclockObject)
            {
                if (_mergedMessagesDic.Count == 0)
                {
                    return;
                }

                foreach (Embed embed in _mergedMessagesDic.Values)
                {
                    QueueMergedLogMessages(embed);
                }

                _mergedMessagesDic.Clear();
            }
        }

        private void MergeLogMessage(LogMessageItem logMessageItem)
        {
            lock (_mergeDiclockObject)
            {
                string messageType = CombineMessageTypes ? "combined" : logMessageItem.MessageType;

                if (_mergedMessagesDic.ContainsKey(messageType))
                {
                    Embed mergedItem = _mergedMessagesDic[messageType];

                    mergedItem.Fields.Add(new EmbedField
                    {
                        Inline = false,
                        Name = $"{logMessageItem.Time}",
                        Value = logMessageItem.Message
                    });

                    if (mergedItem.Fields.Count == 25)
                    {
                        QueueMergedLogMessages(mergedItem);
                        _mergedMessagesDic.Remove(messageType);
                    }
                }
                else
                {
                    var embed = new Embed
                    {
                        Title = logMessageItem.MessageType,
                        Color = logMessageItem.Color,
                        Fields = new List<EmbedField>()
                    };

                    embed.Fields.Add(new EmbedField
                    {
                        Inline = false,
                        Name = $"{logMessageItem.Time}",
                        Value = logMessageItem.Message
                    });

                    _mergedMessagesDic.Add(messageType, embed);
                }
            }
        }

        private void QueueMergedLogMessages(Embed embed)
        {
            var toSend = new WebHook();
            toSend.Embeds.Add(embed);
            QueueSendMessage(toSend);
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