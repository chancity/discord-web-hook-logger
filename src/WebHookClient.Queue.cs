using System;
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
                        webHookClient.MergedMessagesToSendQueue();
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
                            Console.WriteLine(
                                $"Discord notifications have been limited for '{retValue.RateLimitResponse.RetryAfter}ms'");

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
    }
}