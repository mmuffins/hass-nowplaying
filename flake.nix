{
  description = "hass-nowplaying";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs = { self, nixpkgs, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      lib = pkgs.lib;
      appVersion = "2.0.0";
      dotnetVersion = "10_0";

      filteredSrc = lib.cleanSourceWith {
        src = self;
        filter = path: type:
          let
            rel = lib.removePrefix (toString self + "/") (toString path);
          in
            !lib.hasPrefix ".config/" rel
            && rel != ".config";
      };

      hass-nowplaying = pkgs.buildDotnetModule {
        pname = "hass-nowplaying";
        version = appVersion;

        meta = with lib; {
          description = "Home Assistant Now Playing Daemon";
          license = licenses.mit;
          platforms = [ system ];
          mainProgram = "hass-nowplaying";
        };

        dotnet-sdk = pkgs.dotnetCorePackages."sdk_${dotnetVersion}";
        dotnet-runtime = pkgs.dotnetCorePackages."runtime_${dotnetVersion}";

        src = filteredSrc;
        projectFile = [ "NowPlayingDaemon/NowPlayingDaemon.csproj" ];

        # to manually update dependencies:
        # dotnet restore --use-current-runtime --packages nuget-restore ./hass_mpris.sln
        # nuget-to-json nuget-restore > deps.json
        # rm -r nuget-restore
        nugetDeps = ./deps.json;
        executables = [ "hass-nowplaying" ];
      };
    in
    {
      packages.${system} = {
        inherit hass-nowplaying;
        default = hass-nowplaying;
      };

      homeManagerModules.hass-nowplaying = { config, lib, pkgs, ... }:
        let
          cfg = config.services.hass-nowplaying;

          generatedSettingsFile = pkgs.writeText "hass-nowplaying-appsettings.json" (
            builtins.toJSON {
              Logging = {
                LogLevel = {
                  Default = cfg.logLevel;
                };
              };

              HomeAssistant = {
                Host = cfg.homeAssistant.host;
                Port = cfg.homeAssistant.port;
                Ssl = cfg.homeAssistant.ssl;
              };

              MediaplayerEntity = cfg.mediaPlayerEntity;
              MediaArtSize = cfg.mediaArtSize;
            }
          );

          effectiveSettingsFile =
            if cfg.settingsFile != null
            then cfg.settingsFile
            else generatedSettingsFile;
        in
        {
          options.services.hass-nowplaying = {
            enable = lib.mkEnableOption "Home Assistant Now Playing Daemon";

            package = lib.mkOption {
              type = lib.types.package;
              default = hass-nowplaying;
              description = "The package to run as the Home Assistant Now Playing Daemon.";
            };

            notifyOnFailure = lib.mkOption {
              type = lib.types.bool;
              default = true;
              description = "Enable notifications when the service fails.";
            };

            settingsFile = lib.mkOption {
              type = lib.types.nullOr lib.types.str;
              default = null;
              description = ''
                Full path to the main appsettings JSON file.

                If null, the module generates a non-secret appsettings.json automatically
                from the other module options.
              '';
            };

            secretSettingsFile = lib.mkOption {
              type = lib.types.nullOr lib.types.str;
              default = null;
              description = ''
                Full path to a JSON file containing secret overrides, such as
                HomeAssistant.Token. This file must not live in the Nix store.
              '';
            };

            logLevel = lib.mkOption {
              type = lib.types.str;
              default = "Warning";
              description = "Default logging level written into the generated appsettings.json.";
            };

            homeAssistant = {
              host = lib.mkOption {
                type = lib.types.str;
                example = "homeassistant.default";
                description = "Home Assistant hostname.";
              };

              port = lib.mkOption {
                type = lib.types.port;
                default = 8123;
                description = "Home Assistant port.";
              };

              ssl = lib.mkOption {
                type = lib.types.bool;
                default = false;
                description = "Whether to use HTTPS for Home Assistant.";
              };
            };

            mediaPlayerEntity = lib.mkOption {
              type = lib.types.str;
              example = "media_player.sonos_arc";
              description = "The Home Assistant Media player entity ID to expose through MPRIS.";
            };

            mediaArtSize = lib.mkOption {
              type = lib.types.int;
              default = 0;
              description = "Artwork size setting passed to the application. Set to 0 to always use the full artwork size.";
            };
          };

          config = lib.mkIf cfg.enable {
            home.packages = [ cfg.package ];

            systemd.user.services.hass-nowplaying = {
              Unit = {
                Description = "Home Assistant Now Playing Daemon";
                Wants = [ "network-online.target" ];
                After = [ "dbus.service" "network-online.target" ];
                Requires = [ "dbus.service" ];
                OnFailure = lib.optional cfg.notifyOnFailure "hass-nowplaying-notify.service";
              };

              Service = {
                ExecStart = lib.getExe cfg.package;
                Restart = "on-failure";
                Environment =
                  [ "HASSNOWPLAYING_APPSETTINGS_PATH=${effectiveSettingsFile}" ]
                  ++ lib.optional (cfg.secretSettingsFile != null)
                    "HASSNOWPLAYING_SECRET_APPSETTINGS_PATH=${cfg.secretSettingsFile}";
              };

              Install = {
                WantedBy = [ "default.target" ];
              };
            };

            systemd.user.services.hass-nowplaying-notify = lib.mkIf cfg.notifyOnFailure {
              Unit = {
                Description = "Notify user if hass-nowplaying fails";
                After = [ "graphical-session.target" ];
              };

              Service = {
                Type = "oneshot";
                ExecStart =
                  "${pkgs.libnotify}/bin/notify-send --urgency=critical --app-name=hass-nowplaying --icon=dialog-error \"Home Assistant Now Playing Daemon failed. See systemctl --user status hass-nowplaying\"";
              };
            };
          };
        };
    };
}
