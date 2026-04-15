# Home Assistant Now Playing Daemon

`hass-nowplaying` connects a local Linux desktop to a Home Assistant instance and exposes a media player over D-Bus / MPRIS. This lets the local desktop display information about the currently playing media to be displayed on the and allows for remote control of the media player.

# Setup
## Manual installation
Download the latest tarball from the releases section and extract it:

```bash
tar -xzvf hass-nowplaying.tar.gz
```
Copy the binary to `/usr/bin`:

```bash
cp hass-nowplaying/hass-nowplaying /usr/bin
```

Create the config directory and copy the example config:

```bash
mkdir -p ~/.config/hass-nowplaying
cp hass-nowplaying/appsettings.json ~/.config/hass-nowplaying/
```

Optionally create a separate secrets file for the Home Assistant token:

```bash
cp hass-nowplaying/appsettings.secrets.json ~/.config/hass-nowplaying/
```

## Via DEB package

Install:

```bash
dpkg -i hass-nowplaying.deb
```

Copy the example config:

```bash
mkdir -p ~/.config/hass-nowplaying/
cp /usr/share/doc/hass-nowplaying/appsettings.json ~/.config/hass-nowplaying/
```

## Via RPM package

Install:

```bash
rpm -ivh hass-nowplaying-<version>.rpm
```

Copy the example config:

```bash
mkdir -p ~/.config/hass-nowplaying/
cp /usr/share/doc/hass-nowplaying/appsettings.json ~/.config/hass-nowplaying/
```

---

# Configuration

The application uses standard .NET configuration and supports layered configuration.

## Configuration file lookup order

### Main config file

The main `appsettings.json` is loaded in this order:

1. `HASSNOWPLAYING_APPSETTINGS_PATH`
2. `$XDG_CONFIG_HOME/hass-nowplaying/appsettings.json`
3. `/etc/hass-nowplaying/appsettings.json` (when running as root)
4. `~/.config/hass-nowplaying/appsettings.json`

### Secret config file

An optional secret override file is loaded in this order:

1. `HASSNOWPLAYING_SECRET_APPSETTINGS_PATH`
2. `appsettings.secrets.json` in the same directory as the main config file

See `appsettings.json` and `appsettings.secrets.json` in the root of this repository for examples on how to structure the settings file.

## Supported settings

### Logging

Standard .NET logging configuration is supported.

See Microsoft documentation for details.

### HomeAssistant

- `Host`: hostname or IP address of the Home Assistant instance
- `Port`: Home Assistant port
- `Ssl`: whether HTTPS should be used
- `Token`: long-lived access token (recommended to place in secrets file)

### Other settings

- `MediaplayerEntity`: Home Assistant media player entity ID to expose over MPRIS
- `MediaArtSize`: cover art size in pixels. Set to 0 to always use the original artwork size.

---

# Running the application

## Interactive mode

Once installed and configured, start it manually:

```bash
hass-nowplaying
```

The application will create an MPRIS media player service and integrate automatically with supported desktop environments.

## systemd user service

> This application requires access to the user D-Bus session and must run as a user service, not as a system service.

Install the service file:

```bash
cp /usr/share/hass-nowplaying/hass-nowplaying.service ~/.config/systemd/user/
systemctl --user daemon-reload
```

Start it:

```bash
systemctl --user start hass-nowplaying.service
```

Enable on login:

```bash
systemctl --user enable hass-nowplaying.service
```

When running the application as daemon it is recommended to change the log level in appsettings.json to `Warning` to prevent spamming the system log with unneeded information.


## Uninstalling the service

```bash
systemctl --user stop hass-nowplaying.service
systemctl --user disable hass-nowplaying.service
rm ~/.config/systemd/user/hass-nowplaying.service
systemctl --user daemon-reload
```

---

# Nix / Home Manager

The application is available as nix flake for home manager. The flake installs the package, supports automatic creation of the settings file, and registers a systemd user service.


Example:

```nix
{
  imports = [
    inputs.hass-nowplaying.homeManagerModules.hass-nowplaying
  ];

  services.hass-nowplaying = {
    enable = true;

    homeAssistant.host = "homeassistant.default";
    homeAssistant.port = 8123;
    homeAssistant.ssl = false;

    mediaPlayerEntity = "media_player.sonos_arc";
    mediaArtSize = 0;

    tokenFile = "path-to-token-file";
  };
}
```

Note that when enabling the service, the secret settings file should only contain the token itsef, without the recommended json structure.

---

# Upgrading

## RPM upgrade

```bash
systemctl --user stop hass-nowplaying.service
rpm -Uvh hass-nowplaying-<version>.rpm
systemctl --user start hass-nowplaying.service
```

---

# Development

## Updating Home Assistant entities

```bash
dotnet tool restore
dotnet tool run nd-codegen \
  -host homeassistant.default \
  -port 8123 \
  -ssl false \
  -ns hass_mpris.HassClasses \
  -o ./NowPlayingDaemon/Classes/MassEntity.cs \
  -token "TOKEN"
```
Note that after generating the home assistant entities only the relevant media player definitions were kept to keep the class structure lean.

## Local debugging

For local development, you can use:

* `appsettings.Development.json`
* `appsettings.secret.Development.json`

And point the application to them using:

* `HASSNOWPLAYING_APPSETTINGS_PATH`
* `HASSNOWPLAYING_SECRET_APPSETTINGS_PATH`
