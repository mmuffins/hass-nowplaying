{ lib
, fetchFromGitHub
, buildDotnetModule
, dotnetCorePackages
}:

buildDotnetModule rec {
  pname = "hass-nowplaying";
  version = "1.0.671";

  meta = with lib; {
    description = "Home Assistant Now Playing Daemon";
    license = licenses.mit;
    platforms = platforms.linux;
  };

  src = ./.;

  projectFile = "NowPlayingDaemon/NowPlayingDaemon.csproj";
  nugetDeps = ./deps.json;
  executables = [ "hass-nowplaying" ];

  dotnet-sdk = dotnetCorePackages.sdk_8_0;
  dotnet-runtime = dotnetCorePackages.runtime_8_0;
}