using System;
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
        public static int RateLimitMs { get; set; }

        private static readonly HttpClient HttpClient;
        private static readonly ConcurrentQueue<WebHook> ToSendQueue;
        private static readonly ConcurrentQueue<LogMessageItem> LogMessageQueue;
        private static readonly int DefaultColor;
        private static readonly HashSet<WebHookClient> WebHookClients;
        private static readonly object LockObj;

        static WebHookClient()
        {
            HttpClient = new HttpClient();
            LockObj = new object();
            WebHookClients = new HashSet<WebHookClient>();
            DefaultColor = Color.Purple.ToRgb();
            RateLimitMs = 10000;

            ToSendQueue = new ConcurrentQueue<WebHook>();
            LogMessageQueue = new ConcurrentQueue<LogMessageItem>();

            var sendWebHooksThread = new Task(QueueThread, TaskCreationOptions.LongRunning);
            sendWebHooksThread.Start();
        }
        private static async Task<WebHookResponse> _Send(WebHook toSend, string fileData = null)
        {
            MultipartFormDataContent form = null;
            MemoryStream stream = null;
            StreamContent streamContent = null;
            ByteArrayContent fileStringContent = null;
            StringContent stringContent = null;

            try
            {
                form = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(fileData))
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(fileData);
                    stream = new MemoryStream(byteArray);
                    streamContent = new StreamContent(stream);

                    fileStringContent = new ByteArrayContent(streamContent.ReadAsByteArrayAsync().Result);
                    fileStringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    form.Add(fileStringContent, "file", "error.log");
                }

                stringContent = new StringContent(toSend.PayloadJson, Encoding.UTF8, "application/json");
                form.Add(stringContent, "payload_json");

                using (HttpResponseMessage response = await HttpClient.PostAsync(toSend.WebHookUrl, form).ConfigureAwait(false))
                {
                    var retString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if ((int)response.StatusCode != 429)
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
    }
}
