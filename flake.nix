{
  description = "hass-nowplaying";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { self, nixpkgs, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      appVersion = "1.0.1301";
      dotnetVersion = "8_0";
    in
    {
      inherit system;

      packages."${system}" = {
        hass-nowplaying = pkgs.buildDotnetModule rec {
          pname = "hass-nowplaying";
          version = appVersion;


          meta = with pkgs.lib; {
            description = "Home Assistant Now Playing Daemon";
            license = licenses.mit;
            platforms = [ system ];
            mainProgram = "hass-nowplaying";
          };

          dotnet-sdk = pkgs.dotnetCorePackages."sdk_${dotnetVersion}";
          dotnet-runtime = pkgs.dotnetCorePackages."runtime_${dotnetVersion}";

          src = self;

          projectFile = [
            "NowPlayingDaemon/NowPlayingDaemon.csproj"
          ];

          # to manually update dependencies:
          # dotnet restore --use-current-runtime --packages nuget-restore ./ProcessTracker.sln
          # nuget-to-json nuget-restore > deps.json
          # rm -r nuget-restore
          nugetDeps = "${src}/deps.json";
          executables = [ "hass-nowplaying" ];
        };
      };

      defaultPackage."${system}" = self.packages."${system}".hass-nowplaying;

      nixosModules.hass-nowplaying =
        { config, lib, ... }:
        let
          cfg = config.services.hass-nowplaying;
        in
        {
          options.services.hass-nowplaying = {
            enable = lib.mkEnableOption "Enable the hass-nowplaying user service";

            package = lib.mkOption {
              type = lib.types.package;
              default = self.packages.${system}.hass-nowplaying;
              description = "Home Assistant Now Playing Daemon";
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
              };

              Service = {
                ExecStart = "${lib.getExe' cfg.package "hass-nowplaying"}";
                Restart = "on-failure";
              };

              Install.WantedBy = [ "default.target" ];
            };
          };
        };
    };
}
