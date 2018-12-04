using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using discord_web_hook_logger.Extensions;
using discord_web_hook_logger.Models;
using Newtonsoft.Json;

namespace discord_web_hook_logger
{
    public partial class WebHookClient
    {
        private static readonly HttpClient HttpClient;
        private static readonly ConcurrentQueue<WebHook> ToSendQueue;
        private static readonly int DefaultColor;
        private static readonly HashSet<WebHookClient> WebHookClients;
        private static readonly object LockObj;

        static WebHookClient()
        {
            LockObj = new object();
            WebHookClients = new HashSet<WebHookClient>();
            HttpClient = new HttpClient();
            DefaultColor = Color.Purple.ToRgb();
            RateLimitMs = 10000;

            ToSendQueue = new ConcurrentQueue<WebHook>();

            var sendWebHooksThread = new Task(QueueThread, TaskCreationOptions.LongRunning);
            sendWebHooksThread.Start();
        }

        public static int RateLimitMs { get; set; }
        public static int SendingQueueSize => ToSendQueue.Count;


        private static async Task<WebHookResponse> _Send(WebHook toSend)
        {
            MultipartFormDataContent form = null;
            MemoryStream stream = null;
            StreamContent streamContent = null;
            ByteArrayContent fileStringContent = null;
            StringContent stringContent = null;

            try
            {
                form = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(toSend.FileData))
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(toSend.FileData);
                    stream = new MemoryStream(byteArray);
                    streamContent = new StreamContent(stream);

                    fileStringContent = new ByteArrayContent(streamContent.ReadAsByteArrayAsync().Result);
                    fileStringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    form.Add(fileStringContent, "file", "error.log");
                }

                stringContent = new StringContent(toSend.PayloadJson, Encoding.UTF8, "application/json");
                form.Add(stringContent, "payload_json");

                using (HttpResponseMessage response =
                    await HttpClient.PostAsync(toSend.WebHookUrl, form).ConfigureAwait(false))
                {
                    string retString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if ((int) response.StatusCode != 429)
                    {
                        return new WebHookResponse(retString);
                    }

                    var rateLimitResponse = JsonConvert.DeserializeObject<DiscordRateLimitResponse>(retString);

                    return new WebHookResponse(retString, rateLimitResponse);
                }
            }
            catch (WebException e)
            {
                e?.Response?.Dispose();

                throw;
            }
            finally
            {
                form?.Dispose();
                stream?.Dispose();
                stringContent?.Dispose();
                fileStringContent?.Dispose();
                streamContent?.Dispose();
            }

            
        }

        public static class Factory
        {
            private static readonly ConcurrentDictionary<string, WebHookClient> FactoryWebHookClients;
           
            static Factory()
            {
                FactoryWebHookClients = new ConcurrentDictionary<string, WebHookClient>();
            }

            public static WebHookClient CreateClient(long channelId, string channelToken, Dictionary<string, Color> colorMap = null)
            {
                var webHookUrl = $"https://discordapp.com/api/webhooks/{channelId}/{channelToken}";

                if (!FactoryWebHookClients.ContainsKey(webHookUrl))
                {
                    FactoryWebHookClients.TryAdd(webHookUrl, new WebHookClient(webHookUrl));
                }

                var ret = FactoryWebHookClients[webHookUrl];

                ret.UpdateColorMap(colorMap);

                lock (LockObj)
                {
                    WebHookClients.Add(ret);
                }

                return ret;
            }
        }
    }
}