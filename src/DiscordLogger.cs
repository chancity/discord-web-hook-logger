using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace discord_web_hook_logger
{

    public interface IDiscordLogger : ILogger
    {
        WebHookClient WebHookClient { get; }
    }

    public interface IDiscordLogger<T> : ILogger<T>, IDiscordLogger { }

    public class DiscordLogger<T> : IDiscordLogger<T>
    {
        private static readonly Dictionary<string, Color> ColorMap;

        private readonly ILogger _logger;
        private readonly string _type;

        static DiscordLogger()
        {
            ColorMap = new Dictionary<string, Color>
            {
                {LogLevel.Trace.ToString(), Color.White},
                {LogLevel.Debug.ToString(), Color.Aquamarine},
                {LogLevel.Information.ToString(), Color.MediumPurple},
                {LogLevel.Warning.ToString(), Color.Yellow},
                {LogLevel.Error.ToString(), Color.Orange},
                {LogLevel.Critical.ToString(), Color.Red},
                {LogLevel.None.ToString(), Color.DarkViolet}
            };
        }

        public DiscordLogger(ILoggerFactory factory, long channelId, string channelToken,
                             Dictionary<string, Color> colorMap)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            string typeName = TypeNameHelper.GetTypeDisplayName(typeof(T));
            _type = typeof(T).Name;
            _logger = factory.CreateLogger(typeName);


            if (colorMap == null)
            {
                colorMap = ColorMap;
            }

            
            WebHookClient = WebHookClient.Factory.CreateClient(channelId,channelToken, colorMap);
        }

        public WebHookClient WebHookClient { get; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                                Func<TState, Exception, string> formatter)
        {
            string description = GetLogDescription(logLevel, eventId, state, exception, formatter);

            if (logLevel == LogLevel.Critical || logLevel == LogLevel.Error)
            {
                WebHookClient.SendLogMessage(logLevel.ToString(), description, exception);
            }
            else
            {
                WebHookClient.QueueLogMessage(logLevel.ToString(), description, exception);
            }

            //this._logger.Log<TState>(logLevel, eventId, state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        private string GetLogDescription<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                                                 Func<TState, Exception, string> formatter)
        {
            string formatted = formatter.Invoke(state, exception);
            string description = $"**({_type})** {formatted}";

            if (!string.IsNullOrEmpty(exception?.Message))
            {
                description += $"{(string.IsNullOrEmpty(formatted) ? "" : ". ")}" + $"Ex: {exception.Message}";
            }

            return description;
        }
    }
}