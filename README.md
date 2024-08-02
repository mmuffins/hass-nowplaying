# Home Assistant Now Playing Daemon

This tool connects to home assistant and integrates with the linux D-Bus to display information about the currently playing media on media player entities.

## Development Setup
config file locations
appsettings parameters
  home assistant
  player name

running as deamon
service file

mpris desktop file


# Configuration
The application is configured in the appsettings.json which it tries to find in different locations, in the following order:
- The path configured in the `HASSNOWPLAYING_CONFIG_PATH` environment variable
- `$XDG_CONFIG_HOME/hass-nowplaying/appsettings.json` if the XDG_CONFIG_HOME environment variable is set
- `/etc/hass-nowplaying/appsettings.json` if running as root
- `~/.config/hass-nowplaying/appsettings.json` if no other option applies

The following options are supported in the configuration file:
- `Logging` - Optional. Supports standard .net core logging settings, see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging
- `HomeAssistant`
  - `Host` - The hostname or IP address of the Home Assistant instance to connect to.
  - `Port` - The port number on which Home Assistant is running.
  - `Ssl` - Boolean value indicating whether to use SSL/TLS to connect to Home Assistant.
  - `Token` - Long-lived access to authenticate with Home Assistant.
- `MediaplayerEntity` - The entity ID of the media player in Home Assistant to connect to.


Also see the included appsettings.json example file.



The application can be executed interactively, and supports running as deamon using user or system-wide configuration files. In all cases the configuration 




# System-wide configuration
When running the application as daemon




# Running the application
# HOW TO RUN INTERACTIVELY

## Running as daemon
The application supports being executed as daemon with systemd


commands:

systemctl --user daemon-reload
systemctl --user start hassnowplaying.service
systemctl --user enable hassnowplaying.service


sudo systemctl daemon-reload
sudo systemctl start hassnowplaying.service
sudo systemctl enable hassnowplaying.service


hassnowplaying.service in ~/.config/systemd/user/ or /etc/systemd/service