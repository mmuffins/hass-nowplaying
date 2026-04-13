{
  description = "hass-nowplaying";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs = { self, nixpkgs, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      lib = pkgs.lib;
      appVersion = "1.0.1351";
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
          };

          config = lib.mkIf cfg.enable {
            home.packages = [ cfg.package ];

            systemd.user.services.hass-nowplaying = {
              Unit = {
                Description = "Home Assistant Now Playing Daemon";
                Wants = [ "network-online.target" ];
                After = [ "dbus.service network-online.target" ];
                Requires = [ "dbus.service" ];

                OnFailure = lib.optional cfg.notifyOnFailure "hass-nowplaying-notify.service";
              };

              Service = {
                ExecStart = lib.getExe cfg.package;
                Restart = "on-failure";
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