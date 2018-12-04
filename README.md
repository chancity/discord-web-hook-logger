# discord-web-hook-logger

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using discord_web_hook_logger;
using discord_web_hook_logger.Extensions;
using discord_web_hook_logger.Models;
using Microsoft.Extensions.Logging;

namespace example
{
    class Program
    {
        private static readonly WebHookClient DiscordWebHookClient;
        private static readonly IDiscordLogger Logger;
        private static readonly long _discordChannelId = 519560492172181519;
        private static readonly string _discordChannelToken = "p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6";

        static Program()
        {
            //With Logger factory
            Logger = DicordLogFactory.GetLogger<Program>(_discordChannelId, _discordChannelToken);

            //Color of messageType
            Dictionary<string, Color> colorMap = new Dictionary<string, Color>
            {
                {"QueuedLogMessage", Color.DeepPink},
                {"SendLogMessage", Color.Green},
            };

            //Without logger factory
            DiscordWebHookClient = WebHookClient.Factory.CreateClient(_discordChannelId, _discordChannelToken, colorMap);

            //If set to true all logs level will aggregate into a single message
            //Setting to false will seperate log levels into their own message
            DiscordWebHookClient.CombineMessageTypes = false;


            //Log can take N seconds to appear, adjust using WebHookClient.RateLimitMs
            WebHookClient.RateLimitMs = 10000;
        }

        //Webhook URL  https://discordapp.com/api/webhooks/519560492172181519/p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6
        //Id 519560492172181519
        //Token p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6
        static void Main(string[] args)
        {
            SendWithLoggerFactory();
            SendWithoutLoggerFactory();

            Console.ReadLine();
        }
        static void SendWithLoggerFactory()
        {
            Logger.LogCritical("Test Critical Log");
            Logger.LogError("Test Error Log");
            Logger.LogDebug("Test Debug Log");
            Logger.LogWarning("Test Warning Log");
            Logger.LogInformation("Test Information Log");
            Logger.LogTrace("Test Trace Log");
        }

        static void SendWithoutLoggerFactory()
        {
            DiscordWebHookClient.QueueLogMessage("QueuedLogMessage", "message", new Exception());

            //Sends immediately
            DiscordWebHookClient.SendLogMessage("SendLogMessage", "message", new Exception());


            var embed1 = new Embed
            {
                Title = "QueuedMessage",
                Color = Color.Aqua.ToRgb()
            };

            embed1.Fields.Add(new EmbedField
            {
                Inline = false,
                Name = $"{DateTime.Now}",
                Value = "Message"
            });

            var toSend1 = new WebHook
            {
                FileData = "Some string error message thats too big for a message"
            };
            toSend1.Embeds.Add(embed1);

            //This queues the log for sending
            DiscordWebHookClient.QueueSendMessage(toSend1);

            var embed2 = new Embed
            {
                Title = "SendMessage",
                Color = Color.Aqua.ToRgb()
            };

            embed2.Fields.Add(new EmbedField
            {
                Inline = false,
                Name = $"{DateTime.Now}",
                Value = "Message"
            });

            var toSend2 = new WebHook
            {
                FileData = "Some string error message thats too big for a message"
            };

            toSend2.Embeds.Add(embed2);

            //This will send the log immediately 
            DiscordWebHookClient.SendMessage(toSend2);
        }
    }
}