{ pkgs ? import <nixpkgs> {} }:
let
  buildInputs = with pkgs; [
    dotnetCorePackages.sdk_8_0
    omnisharp-roslyn
    nuget-to-json
    # stdenv.cc
  ];
in
  pkgs.mkShell {
  inherit buildInputs;
    name = "dotnet-env";

    DOTNET_ROOT = "${pkgs.dotnetCorePackages.sdk_8_0}";

    shellHook = ''
      export LD_LIBRARY_PATH=${pkgs.lib.makeLibraryPath buildInputs}
      exec zsh
    '';
}