using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using discord_web_hook_logger.Models;

namespace discord_web_hook_logger
{
    public partial class WebHookClient
    {
        private static async void QueueThread()
        {
            while (true)
            {
                lock (LockObj)
                {
                    foreach (WebHookClient webHookClient in WebHookClients)
                    {
                        webHookClient.MergeMessages();
                    }
                }


                if (ToSendQueue.TryDequeue(out WebHook toSend))
                {
                    WebHookResponse retValue = null;

                    try
                    {
                        retValue = await _Send(toSend).ConfigureAwait(false);

                        if (retValue.RateLimitResponse != null)
                        {
                            Console.WriteLine($"Discord notifications have been limited for '{retValue.RateLimitResponse.RetryAfter}ms'");
                            await Task.Delay(retValue.RateLimitResponse.RetryAfter).ConfigureAwait(false);
                        }
                        else
                        {
                            await Task.Delay(RateLimitMs).ConfigureAwait(false);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e.Message} data returned '{retValue?.ReturnValue}'");
                    }
                }

                await Task.Delay(1).ConfigureAwait(false);
            }
        }
        private void MergeMessages()
        {
            if (CombineMessageTypes)
            {
                MergeAllMessageTypes();
            }
            else
            {
                MergeIndividualMessageTypes();
            }
        }
        private void MergeIndividualMessageTypes()
        {
            if (LogMessageQueue.IsEmpty)
            {
                return;
            }

            var queueItemMessagesDic = new Dictionary<string, Embed>();

            while (!LogMessageQueue.IsEmpty)
            {
                if (!LogMessageQueue.TryDequeue(out LogMessageItem queueItem))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(queueItem.StackTrace))
                {
                    var embed = new Embed
                    {
                        Title = queueItem.MessageType,
                        Color = queueItem.Color
                    };


                    embed.Fields.Add(new EmbedField
                    {
                        Inline = false,
                        Name = $"{queueItem.Time}",
                        Value = queueItem.Message
                    });

                    EnqueueLog(embed, queueItem.StackTrace);
                }

                if (queueItemMessagesDic.ContainsKey(queueItem.MessageType))
                {
                    queueItemMessagesDic[queueItem.MessageType].Fields.Add(new EmbedField
                    {
                        Inline = false,
                        Name = $"{queueItem.Time}",
                        Value = queueItem.Message
                    });
                }
                else
                {
                    var embed = new Embed
                    {
                        Title = queueItem.MessageType,
                        Color = queueItem.Color,
                        Fields = new List<EmbedField>()
                    };

                    embed.Fields.Add(new EmbedField
                    {
                        Inline = false,
                        Name = $"{queueItem.Time}",
                        Value = queueItem.Message
                    });

                    queueItemMessagesDic.Add(queueItem.MessageType, embed);
                }

                if (queueItemMessagesDic[queueItem.MessageType].Fields.Count == 25)
                {
                    EnqueueLog(queueItemMessagesDic[queueItem.MessageType]);
                }
            }


            foreach (string key in queueItemMessagesDic.Keys)
            {
                EnqueueLog(queueItemMessagesDic[key]);
            }
        }
        private void MergeAllMessageTypes()
        {
            if (LogMessageQueue.IsEmpty)
            {
                return;
            }

            Embed embed = null;
            while (!LogMessageQueue.IsEmpty)
            {
                if (!LogMessageQueue.TryDequeue(out LogMessageItem queueItem))
                {
                    continue;
                }

                if (embed == null)
                {
                    embed = new Embed
                    {
                        Color = DefaultColor,
                        Fields = new List<EmbedField>()
                    };
                }

                embed.Fields.Add(new EmbedField
                {
                    Inline = false,
                    Name = $"{queueItem.Time}",
                    Value = $"**[{queueItem.MessageType}]** "+queueItem.Message
                });

                if (!string.IsNullOrEmpty(queueItem.StackTrace))
                {
                    EnqueueLog(embed, queueItem.StackTrace);
                    embed = null;
                    continue;
                }

                if (embed.Fields.Count == 25)
                {
                    EnqueueLog(embed);
                    embed = null;
                }
            }

            if (embed != null)
            {
                EnqueueLog(embed);
            }
        }
        private void EnqueueLog(Embed embed, string stackTrace = null)
        {
            var toSend = new WebHook
            {
                WebHookUrl = WebHookUrl,
                FileData = stackTrace
            };

            toSend.Embeds.Add(embed);
            ToSendQueue.Enqueue(toSend);
        }
    }
}
