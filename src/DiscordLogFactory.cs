using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;

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


        public static ILogger<T> GetLogger<T>(ulong id, string token, Dictionary<string, Color> colorMap = null)
        {
            return new DiscordLogger<T>(LoggerFactory, id, token, colorMap);
        }
    }

    public class DiscordLogger<T> : ILogger<T>
    {
        private static readonly Dictionary<string, Color> ColorMap = new Dictionary<string, Color>
        {
            {LogLevel.Trace.ToString(), Color.White},
            {LogLevel.Debug.ToString(), Color.Aquamarine},
            {LogLevel.Information.ToString(), Color.MediumPurple},
            {LogLevel.Warning.ToString(), Color.Yellow},
            {LogLevel.Error.ToString(), Color.Orange},
            {LogLevel.Critical.ToString(), Color.Red},
            {LogLevel.None.ToString(), Color.DarkViolet}
        };

        private readonly ILogger _logger;
        private readonly string _type;
        private readonly WebHookClient _webHookClient;

        public DiscordLogger(ILoggerFactory factory, ulong id, string token, Dictionary<string, Color> colorMap)
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


            _webHookClient = new WebHookClient(id, token, colorMap)
            {
                MergeAllTypes = false
            };
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                                Func<TState, Exception, string> formatter)
        {
            string formatted = formatter.Invoke(state, exception);
            string description = $"**({_type})** {formatted}";

            if (!string.IsNullOrEmpty(exception?.Message))
            {
                description += $"{(string.IsNullOrEmpty(formatted) ? "" : ". ")}" + $"Ex: {exception.Message}";
            }

            if (logLevel == LogLevel.Critical)
            {
                _webHookClient.ForceSendLogMessage(logLevel.ToString(), description, exception?.StackTrace);
                Environment.Exit(0);
            }

            if (logLevel >= LogLevel.Error)
            {
                _webHookClient.QueueLogMessage(logLevel.ToString(), description, exception?.StackTrace);
            }
            else
            {
                _webHookClient.QueueLogMessage(logLevel.ToString(), description);
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
    }
}