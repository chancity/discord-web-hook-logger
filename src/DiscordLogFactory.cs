using System.Collections.Generic;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace discord_web_hook_logger
{
    public class DicordLogFactory
    {
        private static ILoggerFactory _factory;

        public static ILoggerFactory LoggerFactory
        {
            get
            {
                if (_factory != null)
                {
                    return _factory;
                }

                _factory = new LoggerFactory();
                ConfigureLogger(_factory);

                return _factory;
            }
            set => _factory = value;
        }

        public static void ConfigureLogger(ILoggerFactory factory)
        {
            factory.AddDebug(Filter);
        }

        private static bool Filter(string arg1, LogLevel arg2)
        {
            return true;
        }


        public static IDiscordLogger GetLogger<T>(long channelId, string channelToken,
                                                  Dictionary<string, Color> colorMap = null)
        {
            return new DiscordLogger<T>(LoggerFactory, channelId, channelToken, colorMap);
        }
    }
}