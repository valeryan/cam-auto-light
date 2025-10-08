using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CamAutoLight.Interfaces;
using Microsoft.Extensions.Logging;

namespace CamAutoLight.Services;

public class WindowsCameraMonitorService : ICameraMonitorService
{
    private readonly IElgatoLightService _elgatoLightService;
    private readonly ILogger<WindowsCameraMonitorService> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationTokenSource? _monitoringCancellationTokenSource;
    private Timer? _pollingTimer;

    public WindowsCameraMonitorService(
        IElgatoLightService elgatoLightService,
        ILogger<WindowsCameraMonitorService> logger
    )
    {
        _elgatoLightService = elgatoLightService;
        _logger = logger;
    }

    public void CheckInitialCameraState()
    {
        _logger.LogInformation("Checking initial camera state on Windows...");

        try
        {
            bool cameraInUse = IsCameraInUse();

            if (cameraInUse)
            {
                _logger.LogInformation("Camera is ON at startup.");
                _elgatoLightService.TurnOnLights();
            }
            else
            {
                _logger.LogInformation("Camera is OFF at startup.");
                _elgatoLightService.TurnOffLights();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking initial camera state");
        }
    }

    public void MonitorLogStream()
    {
        _logger.LogInformation("Starting Windows camera monitoring via process polling...");

        _monitoringCancellationTokenSource = new CancellationTokenSource();
        var token = _monitoringCancellationTokenSource.Token;

        // Poll for camera usage every 2 seconds
        _pollingTimer = new Timer(
            CheckCameraStateChange,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2)
        );

        // Keep the monitoring running
        Task.Run(
            async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Camera monitoring cancelled");
                }
            },
            token
        );
    }

    private bool _lastCameraState = false;

    private void CheckCameraStateChange(object? state)
    {
        try
        {
            bool currentCameraState = IsCameraInUse();

            if (currentCameraState != _lastCameraState)
            {
                if (currentCameraState)
                {
                    _logger.LogInformation("[EVENT] Camera turned ON");
                    HandleCameraOn();
                }
                else
                {
                    _logger.LogInformation("[EVENT] Camera turned OFF");
                    HandleCameraOff();
                }

                _lastCameraState = currentCameraState;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking camera state change");
        }
    }

    private bool IsCameraInUse()
    {
        try
        {
            // Check for common camera processes
            var cameraProcesses = new[]
            {
                "teams",
                "zoom",
                "skype",
                "discord",
                "obs64",
                "obs32",
                "chrome",
                "firefox",
                "edge",
                "msedge",
                "WindowsCamera",
            };

            foreach (var processName in cameraProcesses)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    _logger.LogDebug($"Found camera process: {processName}");
                    return true;
                }
            }

            // Check for any process with "camera" in the name
            var allProcesses = Process.GetProcesses();
            var cameraRelatedProcesses = allProcesses
                .Where(p =>
                    p.ProcessName.Contains("camera", StringComparison.OrdinalIgnoreCase)
                    || p.ProcessName.Contains("webcam", StringComparison.OrdinalIgnoreCase)
                    || p.ProcessName.Contains("cam", StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();

            if (cameraRelatedProcesses.Length > 0)
            {
                _logger.LogDebug(
                    $"Found camera-related processes: {string.Join(", ", cameraRelatedProcesses.Select(p => p.ProcessName))}"
                );
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if camera is in use");
            return false;
        }
    }

    private void HandleCameraOn()
    {
        // Cancel any pending "off" event
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;

        _elgatoLightService.TurnOnLights();
    }

    private void HandleCameraOff()
    {
        _logger.LogInformation("[EVENT] Camera turned OFF, waiting 3s...");

        // Create a new cancellation token source for the "off" event
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        Task.Delay(3000, token)
            .ContinueWith(
                task =>
                {
                    if (!task.IsCanceled && !IsCameraInUse())
                    {
                        _logger.LogInformation(
                            "[CONFIRMED] Camera is still OFF. Turning off lights."
                        );
                        _elgatoLightService.TurnOffLights();
                    }
                },
                token
            );
    }

    public void Dispose()
    {
        _pollingTimer?.Dispose();
        _monitoringCancellationTokenSource?.Cancel();
        _monitoringCancellationTokenSource?.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
