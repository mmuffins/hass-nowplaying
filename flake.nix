{
  description = "hass-nowplaying";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { self, nixpkgs, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      lib = pkgs.lib;
      appVersion = "1.0.1301";
      dotnetVersion = "10_0";

      filteredSrc = lib.cleanSourceWith {
        src = self;
        filter = path: type:
          let
            rel = lib.removePrefix (toString self + "/") (toString path);
          in
            ! lib.hasPrefix ".config/" rel
            && rel != ".config";
      };
    in
    {
      packages.${system} = {
        hass-nowplaying = pkgs.buildDotnetModule rec {
          pname = "hass-nowplaying";
          version = appVersion;

          src = filteredSrc;

          projectFile = [ "NowPlayingDaemon/NowPlayingDaemon.csproj" ];

          # to manually update dependencies:
          # dotnet restore --use-current-runtime --packages nuget-restore ./hass_mpris.sln
          # nuget-to-json nuget-restore > deps.json
          # rm -r nuget-restore
          nugetDeps = ./deps.json;
          executables = [ "hass-nowplaying" ];

          dotnet-sdk = pkgs.dotnetCorePackages."sdk_${dotnetVersion}";
          dotnet-runtime = pkgs.dotnetCorePackages."runtime_${dotnetVersion}";

          meta = with lib; {
            description = "Home Assistant Now Playing Daemon";
            license = licenses.mit;
            platforms = [ system ];
            mainProgram = "hass-nowplaying";
          };
        };

        default = self.packages.${system}.hass-nowplaying;
      };
    };
}