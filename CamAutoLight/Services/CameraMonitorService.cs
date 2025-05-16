using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CamAutoLight.Interfaces;
using Microsoft.Extensions.Logging;

namespace CamAutoLight.Services;

public class CameraMonitorService(
    IElgatoLightService elgatoLightService,
    ILogger<CameraMonitorService> logger
) : ICameraMonitorService
{
    private readonly ILogger<CameraMonitorService> _logger = logger;
    private readonly IElgatoLightService _elgatoLightService = elgatoLightService;
    private CancellationTokenSource? _cancellationTokenSource;

    public void CheckInitialCameraState()
    {
        _logger.LogInformation("Checking initial camera state...");
        string initialState = RunLogCommand(
            [
                "show",
                "--predicate",
                "subsystem == \"com.apple.UVCExtension\" and composedMessage contains \"Post PowerLog\"",
                "--last",
                "1m",
            ]
        );

        if (initialState.Contains("\"VDCAssistant_Power_State\" = On;"))
        {
            _logger.LogInformation("Camera is ON at startup.");
            _elgatoLightService.TurnOnLights();
            return;
        }

        if (initialState.Contains("\"VDCAssistant_Power_State\" = Off;"))
        {
            _logger.LogInformation("Camera is OFF at startup.");
            _elgatoLightService.TurnOffLights();
            return;
        }

        _logger.LogWarning("Could not determine camera state at startup.");
    }

    public void MonitorLogStream()
    {
        _logger.LogInformation("Monitoring macOS log stream for camera events...");

        ProcessStartInfo psi =
            new()
            {
                FileName = "log",
                ArgumentList =
                {
                    "stream",
                    "--predicate",
                    "subsystem == \"com.apple.UVCExtension\" and composedMessage contains \"Post PowerLog\"",
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

        using Process process = new() { StartInfo = psi };
        process.OutputDataReceived += (sender, args) => ProcessLogEvent(args.Data);
        process.ErrorDataReceived += (sender, args) =>
            _logger.LogError("Error received: {ErrorData}", args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();
    }

    private void ProcessLogEvent(string? logEvent)
    {
        if (string.IsNullOrWhiteSpace(logEvent))
        {
            return;
        }

        if (logEvent.Contains("\"VDCAssistant_Power_State\" = On;"))
        {
            _logger.LogInformation("[EVENT] Camera turned ON");

            // Cancel any pending "off" event
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;

            _elgatoLightService.TurnOnLights();
            return;
        }

        if (logEvent.Contains("\"VDCAssistant_Power_State\" = Off;"))
        {
            _logger.LogInformation("[EVENT] Camera turned OFF, waiting 3s...");

            // Create a new cancellation token source for the "off" event
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task.Delay(3000, token)
                .ContinueWith(
                    task =>
                    {
                        if (!task.IsCanceled)
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
    }

    private static string RunLogCommand(List<string> arguments)
    {
        ProcessStartInfo psi =
            new()
            {
                FileName = "log",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using Process process = new() { StartInfo = psi };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}
