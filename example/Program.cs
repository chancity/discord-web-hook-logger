using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using discord_web_hook_logger;
using discord_web_hook_logger.Extensions;
using discord_web_hook_logger.Models;
using Microsoft.Extensions.Logging;

namespace example
{
    class Program
    {
        //Webhook URL  https://discordapp.com/api/webhooks/519560492172181519/p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6
        //Id 519560492172181519
        //Token p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6
        static void Main(string[] args)
        {
            var discordChannelId = 519560492172181519;
            var discordChannelToken = "p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6";


            SendWithLoggerFactory(discordChannelId, discordChannelToken);

            SendWithoutLoggerFactory(discordChannelId,discordChannelToken);

            Console.ReadLine();
        }

        static void SendWithLoggerFactory(long discordChannelId, string discordChannelToken)
        {
            var logger = DicordLogFactory.GetLogger<Program>(discordChannelId, discordChannelToken);
            logger.LogCritical("Test Critical Log");
            logger.LogError("Test Error Log");
            logger.LogDebug("Test Debug Log");
            logger.LogWarning("Test Warning Log");
            logger.LogInformation("Test Information Log");
            logger.LogTrace("Test Trace Log");
        }

        static void SendWithoutLoggerFactory(long discordChannelId, string discordChannelToken)
        {
            //Color of messageType
            Dictionary<string, Color> colorMap = new Dictionary<string, Color>
            {
                {"messageType1", Color.DeepPink},
                {"messageType2", Color.HotPink},
            };

            //Without logger factory
            var webHookClient = new WebHookClient(discordChannelId, discordChannelToken, colorMap)
            {
                //If set to true all logs level will aggregate into a single message
                //Setting to false will seperate log levels into their own message
                CombineMessageTypes = false
            };


            //Log can take N seconds to appear, adjust using WebHookClient.RateLimitMs
            WebHookClient.RateLimitMs = 10000;
            webHookClient.QueueLogMessage("messageType1", "message", new Exception());

            //Sends immediately
            webHookClient.SendLogMessage("messageType2", "message", new Exception());


            var embed = new Embed
            {
                Title = "Title",
                Color = Color.Aqua.ToRgb()
            };

            embed.Fields.Add(new EmbedField
            {
                Inline = false,
                Name = $"{DateTime.Now}",
                Value = "Message"
            });

            var toSend = new WebHook
            {
                FileData = "Some string error message thats too big for a message"
            };

            toSend.Embeds.Add(embed);

            //This queues the log for sending
            webHookClient.QueueMessage(toSend);

            //This will send the log immediately 
            webHookClient.SendMessage(toSend);
        }
    }
}
