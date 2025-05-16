using System;
using CamAutoLight.Config;
using CamAutoLight.Interfaces;
using CamAutoLight.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CamAutoLight
{
    class Program
    {
        static void Main()
        {
            using var serviceProvider = ConfigureServices();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting macOS camera event monitor...");

            try
            {
                // Load and validate config
                var configManager = serviceProvider.GetRequiredService<IConfigManager>();
                configManager.LoadConfig();
                configManager.ValidateConfig();

                // Start monitoring
                var cameraMonitorService =
                    serviceProvider.GetRequiredService<ICameraMonitorService>();
                cameraMonitorService.CheckInitialCameraState();
                cameraMonitorService.MonitorLogStream();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while starting the application.");
                Environment.Exit(1);
            }
        }

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Information))
                .AddSingleton<IConfigManager, ConfigManager>()
                .AddSingleton<IElgatoLightService, ElgatoLightService>()
                .AddSingleton<ICameraMonitorService, CameraMonitorService>()
                .BuildServiceProvider();
        }
    }
}
