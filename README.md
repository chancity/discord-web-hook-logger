# discord-web-hook-logger

```csharp
 class Program
    {
        //Webhook URL  https://discordapp.com/api/webhooks/519560492172181519/p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6
        //Id 519560492172181519
        //Token p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6
        static void Main(string[] args)
        {
            var discordChannelId = 519560492172181519;
            var discordChannelToken = "p9feyLifxedaxy50b8lAmnG3GZZ3lkAjKpJhuJO_gZSeR-9ZwAoStzgqztJ5wU1-cge6";

            var logger = DicordLogFactory.GetLogger<Program>(discordChannelId, discordChannelToken);

            logger.LogCritical("Test Critical Log");
            logger.LogError("Test Error Log");
            logger.LogDebug("Test Debug Log");
            logger.LogWarning("Test Warning Log");
            logger.LogInformation("Test Information Log");
            logger.LogTrace("Test Trace Log");

            Console.ReadLine();
        }
    }
