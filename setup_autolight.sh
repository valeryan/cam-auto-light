#!/bin/bash

# Build the project
sudo dotnet publish ./CamAutoLight/CamAutoLight.csproj -c Release -o /usr/local/bin/camautolight

# Copy the .env file to the target directory
sudo cp ./.env /usr/local/bin/camautolight/

# Copy the plist to the correct location
cp com.camautolight.plist ~/Library/LaunchAgents/com.camautolight.plist

# Load the plist to enable it to launch at login
launchctl unload ~/Library/LaunchAgents/com.camautolight.plist
launchctl load ~/Library/LaunchAgents/com.camautolight.plist

echo "Setup complete. The application will run at login."
