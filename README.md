# Home Assistant Now Playing Daemon 
This tool connects to home assistant and integrates with the linux D-Bus to display information about the currently playing media on media player 
entities.

# Setup
## Manual installation
To manually install the application download the latest tarball from the releases section and extract it with
```bash
tar -xzvf hass-nowplaying.tar.gz
```
Copy the extracted file to the `/usr/bin` directory
```bash
cp hass-nowplaying/hass-nowplaying /usr/bin
```
Copy appsettings json to a valid config file location and configure all required settings. See the configuration section for more information.
```bash
mkdir ~/.config/hass-nowplaying
cp hass-nowplaying/appsettings.json ~/.config/hass-nowplaying
```

## Via deb file
To install the application via deb file, download the latest deb file fro the releases section and install it with
```bash
dpkg -i hass-nowplaying.deb
```
Copy the example configuration file to the user directory
```bash
mkdir ~/.config/hass-nowplaying/
cp /usr/share/doc/hass-nowplaying/appsettings.json ~/.config/hass-nowplaying/
```

# Configuration
## Config file
The application is configured in the appsettings.json which it tries to find in different locations, in the following order:
- The path configured in the `HASSNOWPLAYING_CONFIG_PATH` environment variable
- `$XDG_CONFIG_HOME/hass-nowplaying/appsettings.json` if the XDG_CONFIG_HOME environment variable is set
- `~/.config/hass-nowplaying/appsettings.json` if no other option applies

### Supported properties
The following options are supported in the configuration file:
- `Logging` - Optional. Supports standard .net core logging settings, see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging
- `HomeAssistant`
  - `Host` - The hostname or IP address of the Home Assistant instance to connect to.
  - `Port` - The port number on which Home Assistant is running.
  - `Ssl` - Boolean value indicating whether to use SSL/TLS to connect to Home Assistant.
  - `Token` - Long-lived access to authenticate with Home Assistant.
- `MediaplayerEntity` - The entity ID of the media player in Home Assistant to connect to.

# Running the application
## Interactively
Once it has been installed and configured the application can now be started interactively by running `hass-nowplaying`. There are no parameters or switches, all configuration is done with the configuration file. Once started, the application will create an mpris media player service that automatically integrates itself with all distributions that support it to display the currently playing track and enable media key controls.

## As service
**Note that the application needs to access the user D-Bus session, so it's not possible to run it as regular root service!**
The hass-nowplaying supports being run as service. To do so, copy the service file to the systemd user directory and reload the daemon.
```bash
cp /usr/share/hass-nowplaying/hass-nowplaying.service ~/.config/systemd/user/
systemctl --user daemon-reload
```
The application can now be started as service.
```bash
systemctl --user start hass-nowplaying.service
```
To enable it to run on startup, configure it with
```bash
systemctl --user enable hass-nowplaying.service
```
When running the application as daemon it is recommended to change the log level in appsettings.json to `Warning` to prevent spamming the system log with unneeded information.

To uninstall the service, disable it and delete the service file:
```bash
systemctl --user stop hass-nowplaying.service
systemctl --user disable hass-nowplaying.service
rm ~/.config/systemd/user/hass-nowplaying.service
systemctl --user daemon-reload
```
