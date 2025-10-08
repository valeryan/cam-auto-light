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
        static void Main(string[] args)
        {
            using var serviceProvider = ConfigureServices();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Load and validate config
                var configManager = serviceProvider.GetRequiredService<IConfigManager>();
                configManager.LoadConfig();
                configManager.ValidateConfig();

                // Check for command-line arguments
                if (args.Length > 0)
                {
                    HandleCommand(args[0], serviceProvider, logger);
                    return;
                }

                // Default behavior: start auto monitoring
                logger.LogInformation("Starting camera event monitor...");
                var cameraMonitorServiceFactory =
                    serviceProvider.GetRequiredService<ICameraMonitorServiceFactory>();
                var cameraMonitorService = cameraMonitorServiceFactory.CreateCameraMonitorService();

                cameraMonitorService.CheckInitialCameraState();
                cameraMonitorService.MonitorLogStream();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while starting the application.");
                Environment.Exit(1);
            }
        }

        private static void HandleCommand(
            string command,
            ServiceProvider serviceProvider,
            ILogger<Program> logger
        )
        {
            var lightService = serviceProvider.GetRequiredService<IElgatoLightService>();

            switch (command.ToLower())
            {
                case "--on":
                case "-on":
                case "on":
                    logger.LogInformation("Turning lights ON via command");
                    lightService.TurnOnLights();
                    break;

                case "--off":
                case "-off":
                case "off":
                    logger.LogInformation("Turning lights OFF via command");
                    lightService.TurnOffLights();
                    break;

                case "--toggle":
                case "-toggle":
                case "toggle":
                    logger.LogInformation("Toggling lights via command");
                    lightService.ToggleLights();
                    break;

                case "--help":
                case "-h":
                case "help":
                    ShowHelp();
                    break;

                default:
                    logger.LogError("Unknown command: {command}", command);
                    ShowHelp();
                    Environment.Exit(1);
                    break;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("CamAutoLight - Elgato Key Light Controller");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine(
                "  CamAutoLight                 Start auto camera monitoring (default)"
            );
            Console.WriteLine("  CamAutoLight --on            Turn lights on");
            Console.WriteLine("  CamAutoLight --off           Turn lights off");
            Console.WriteLine("  CamAutoLight --toggle        Toggle lights on/off");
            Console.WriteLine("  CamAutoLight --help          Show this help");
        }

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Information))
                .AddSingleton<IConfigManager, ConfigManager>()
                .AddSingleton<IElgatoLightService, ElgatoLightService>()
                .AddSingleton<ICameraMonitorServiceFactory, CameraMonitorServiceFactory>()
                .AddSingleton<MacOSCameraMonitorService>()
                .AddSingleton<WindowsCameraMonitorService>()
                .BuildServiceProvider();
        }
    }
}
