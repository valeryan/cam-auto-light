# Platform Support for Camera Monitoring

## Overview

CamAutoLight now supports cross-platform camera monitoring using a factory pattern and dependency injection. The application automatically detects the operating system and uses the appropriate monitoring implementation.

## Supported Platforms

### macOS
- **Implementation**: `MacOSCameraMonitorService`
- **Method**: Monitors macOS unified logging system (`log stream`) for UVCExtension events
- **Detection**: Real-time monitoring of camera power state changes
- **Accuracy**: High - directly monitors system camera events

### Windows
- **Implementation**: `WindowsCameraMonitorService`
- **Method**: Process monitoring with periodic polling
- **Detection**: Monitors running processes that commonly use cameras
- **Accuracy**: Good - detects when camera applications are running

## Architecture

### Interface-Driven Design
```csharp
public interface ICameraMonitorService : IDisposable
{
    void CheckInitialCameraState();
    void MonitorLogStream();
}
```

### Factory Pattern
The `ICameraMonitorServiceFactory` creates the appropriate service based on the runtime platform:

```csharp
public interface ICameraMonitorServiceFactory
{
    ICameraMonitorService CreateCameraMonitorService();
}
```

### Dependency Injection
Services are registered in `Program.cs`:
- `ICameraMonitorServiceFactory` → `CameraMonitorServiceFactory`
- `MacOSCameraMonitorService` (platform-specific)
- `WindowsCameraMonitorService` (platform-specific)

## Windows Camera Detection

The Windows implementation monitors for these camera-related processes:
- Microsoft Teams (`teams`)
- Zoom (`zoom`)
- Skype (`skype`)
- Discord (`discord`)
- OBS Studio (`obs64`, `obs32`)
- Web browsers (`chrome`, `firefox`, `edge`, `msedge`)
- Windows Camera app (`WindowsCamera`)
- Any process with "camera", "webcam", or "cam" in the name

### Polling Interval
- Checks every 2 seconds for camera state changes
- 3-second delay before turning off lights (same as macOS)

## Future Enhancements

### Windows
1. **WMI Event Monitoring**: Use Windows Management Instrumentation for real-time process events (requires System.Management package)
2. **Registry Monitoring**: Monitor Windows registry keys for camera access
3. **Windows API Integration**: Use native Windows APIs for more accurate camera state detection
4. **Event Log Monitoring**: Monitor Windows Event Logs for camera-related events

### Linux
1. **V4L2 Monitoring**: Monitor Video4Linux2 device files
2. **D-Bus Integration**: Monitor system D-Bus for camera events
3. **Process Monitoring**: Similar to Windows approach but adapted for Linux

### Cross-Platform
1. **Configuration**: Allow users to specify additional process names to monitor
2. **Performance**: Optimize polling intervals based on system capabilities
3. **Logging**: Enhanced logging for troubleshooting platform-specific issues

## Building and Running

The application automatically selects the correct implementation at runtime. No additional configuration is required for platform-specific features.

### Prerequisites
- **.NET 8.0** or later
- **Windows**: No additional dependencies for basic process monitoring
- **macOS**: Requires access to system logs (usually available by default)

### Optional Dependencies
- **Windows**: `System.Management` package for enhanced WMI monitoring (already included conditionally)
