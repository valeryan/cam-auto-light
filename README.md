# CamAutoLight

Automatically control Elgato Key Lights based on macOS camera usage.

## Overview

**CamAutoLight** is a .NET 8 console application for macOS that monitors the system camera state and automatically turns Elgato Key Lights on or off. When the camera is activated (e.g., by a video call), the lights turn on; when the camera is off, the lights turn off after a short delay.

## Features

- Monitors macOS system logs for camera state changes.
- Controls one or more Elgato Key Lights over the network.
- Configurable brightness and temperature.
- Runs automatically at login via `launchctl`.

## Requirements

- .NET 8 SDK
- macOS
- Elgato Key Light(s) on the same network

## Setup

1. **Clone the repository** and navigate to the project directory.

2. **Configure your lights:**

   Copy `.env.example` to `.env` and edit the values:

   ```env
   ip_addresses=192.168.1.238,192.168.1.239
   brightness=12
   temperature=160
   ```

   - `ip_addresses`: Comma-separated list of Elgato Key Light IPs.
   - `brightness`: Integer (1-100).
   - `temperature`: Integer (143-344).

3. **Build and install:**

   Run the setup script:

   ```sh
   ./autolightctl.sh install
   ```

   This will:
   - Build and publish the app to `/usr/local/bin/camautolight`
   - Copy your `.env` file to the target directory
   - Install the launch agent (`com.camautolight.plist`) to run at login

4. **Verify:**

   The application will now run in the background and control your lights based on camera usage.

## Development

- Open the solution [`CamAutoLight.sln`](CamAutoLight.sln) in Visual Studio or VS Code.
- Use the provided tasks and launch configurations in the `.vscode` folder for building and debugging.

## Project Structure

- [`CamAutoLight/Program.cs`](CamAutoLight/Program.cs): Main entry point.
- [`CamAutoLight/Config/ConfigManager.cs`](CamAutoLight/Config/ConfigManager.cs): Loads and validates configuration.
- [`CamAutoLight/Services/CameraMonitorService.cs`](CamAutoLight/Services/CameraMonitorService.cs): Monitors camera state.
- [`CamAutoLight/Services/ElgatoLightService.cs`](CamAutoLight/Services/ElgatoLightService.cs): Controls the lights.

## Management

Use the [`autolightctl.sh`](autolightctl.sh) script to manage the CamAutoLight service:

```sh
# Install/build and start the service
./autolightctl.sh install

# Restart the running service
./autolightctl.sh restart

# Check if the service is running
./autolightctl.sh status

# Show help and available commands
./autolightctl.sh help

# Completely remove the service
./autolightctl.sh remove
```

**Available commands:**
- `install` - Build, copy files, and load the service
- `restart` - Restart the service
- `remove` - Unload and remove the service
- `status` - Show service status
- `install-route` - Install the VPN route fix (root LaunchDaemon)
- `remove-route` - Remove the VPN route fix
- `help` - Show help message

## VPN route fix (ensure-iot-route)

If the Key Lights live on a separate IoT subnet, a corporate VPN (e.g.
GlobalProtect) can clobber the local route to them, making the lights
unreachable until the route is restored. `ensure-iot-route.sh` fixes this: it
runs as a root LaunchDaemon (`local.ensure-iot-route.plist`) every 60 seconds,
re-adds the route if the VPN has removed it, and kickstarts the
`com.camautolight` service so it reconnects.

The subnet and gateway are configured at the top of the script:

```sh
IOT_NET="192.168.3.0/24"   # subnet the lights are on
IOT_GW="192.168.4.1"       # gateway to reach it
TEST_HOST="192.168.3.3"    # any host in the subnet, used to probe the route
```

Install/remove:

```sh
./autolightctl.sh install-route
./autolightctl.sh remove-route
```

Daemon logs go to `/tmp/local.ensure-iot-route.stdout` / `.stderr`, and to the
system log tagged `ensure-iot-route`.

## Uninstall

To remove the launch agent and application:

```sh
./autolightctl.sh remove
```

If the VPN route fix is installed, also run:

```sh
./autolightctl.sh remove-route
```

## License

MIT License

---

*Inspired by the need for better lighting automation on macOS video calls!*
