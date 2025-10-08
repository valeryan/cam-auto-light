#!/bin/bash

PLIST=~/Library/LaunchAgents/com.camautolight.plist
BIN_DIR=/usr/local/bin/camautolight

function help() {
    echo "Usage: $0 [install|restart|remove|status|logs|on|off|toggle|help]"
    echo ""
    echo "  install   Build, copy files, and load the service"
    echo "  restart   Restart the service"
    echo "  remove    Unload and remove the service"
    echo "  status    Show service status"
    echo "  logs      Show recent log output"
    echo "  on        Turn lights on manually"
    echo "  off       Turn lights off manually"
    echo "  toggle    Toggle lights on/off"
    echo "  help      Show this help message"
}

function install() {
    echo "Building and installing CamAutoLight..."
    sudo dotnet publish ./CamAutoLight/CamAutoLight.csproj -c Release -o $BIN_DIR || exit 1
    sudo cp ./.env $BIN_DIR/

    # Set proper permissions
    sudo chmod +x $BIN_DIR/CamAutoLight
    sudo chmod 755 $BIN_DIR
    sudo chown -R $(whoami):staff $BIN_DIR

    # Ensure dotnet is executable
    sudo chmod +x /usr/local/share/dotnet/dotnet

    cp com.camautolight.plist $PLIST
    launchctl unload $PLIST 2>/dev/null
    launchctl load $PLIST
    echo "Installed and started CamAutoLight."
}

function restart() {
    echo "Restarting CamAutoLight..."
    launchctl unload $PLIST
    launchctl load $PLIST
    echo "Restarted."
}

function remove() {
    echo "Removing CamAutoLight..."
    launchctl unload $PLIST
    rm -f $PLIST
    sudo rm -rf $BIN_DIR
    echo "Removed."
}

function status() {
    launchctl list | grep camautolight && echo "CamAutoLight is loaded." || echo "CamAutoLight is not loaded."
}

function logs() {
    if [ -f "$BIN_DIR/camautolight.log" ]; then
        tail -f "$BIN_DIR/camautolight.log"
    else
        echo "Log file not found. Showing system logs instead:"
        log show --predicate 'process == "CamAutoLight"' --last 1h
    fi
}

function lights_on() {
    if [ ! -f "$BIN_DIR/CamAutoLight" ]; then
        echo "Error: CamAutoLight is not installed. Run './autolightctl.sh install' first."
        exit 1
    fi
    echo "Turning lights on..."
    # Use installed binary for fast execution
    $BIN_DIR/CamAutoLight --on
}

function lights_off() {
    if [ ! -f "$BIN_DIR/CamAutoLight" ]; then
        echo "Error: CamAutoLight is not installed. Run './autolightctl.sh install' first."
        exit 1
    fi
    echo "Turning lights off..."
    # Use installed binary for fast execution
    $BIN_DIR/CamAutoLight --off
}

function lights_toggle() {
    if [ ! -f "$BIN_DIR/CamAutoLight" ]; then
        echo "Error: CamAutoLight is not installed. Run './autolightctl.sh install' first."
        exit 1
    fi
    echo "Toggling lights..."
    # Use installed binary for fast execution
    $BIN_DIR/CamAutoLight --toggle
}

case "$1" in
    install) install ;;
    restart) restart ;;
    remove) remove ;;
    status) status ;;
    logs) logs ;;
    on) lights_on ;;
    off) lights_off ;;
    toggle) lights_toggle ;;
    help|"") help ;;
    *) echo "Unknown command: $1"; help; exit 1 ;;
esac
