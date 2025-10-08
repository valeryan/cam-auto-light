using System;
using System.Runtime.InteropServices;
using CamAutoLight.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CamAutoLight.Services
{
    public class CameraMonitorServiceFactory : ICameraMonitorServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CameraMonitorServiceFactory> _logger;

        public CameraMonitorServiceFactory(
            IServiceProvider serviceProvider,
            ILogger<CameraMonitorServiceFactory> logger
        )
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public ICameraMonitorService CreateCameraMonitorService()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _logger.LogInformation("Creating macOS camera monitor service");
                return _serviceProvider.GetRequiredService<MacOSCameraMonitorService>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogInformation("Creating Windows camera monitor service");
                return _serviceProvider.GetRequiredService<WindowsCameraMonitorService>();
            }
            else
            {
                throw new PlatformNotSupportedException(
                    $"Camera monitoring is not supported on this platform: {RuntimeInformation.OSDescription}. Supported platforms: macOS, Windows"
                );
            }
        }
    }
}
