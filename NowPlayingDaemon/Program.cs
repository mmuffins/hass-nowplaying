using Microsoft.Extensions.Hosting;
using NetDaemon.Runtime;
using NowPlayingDaemon;

var configFilePath = GetConfigFilePath();

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(
        (hostingContext, config) =>
        {
            config.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
        }
    )
    .UseNetDaemonRuntime()
    .UseSystemd()
    .ConfigureServices(
        (_, services) =>
            services
                .AddSingleton<DBusConnectionManager>()
                .AddSingleton<IHassContextProvider, HassContextProvider>()
                .AddSingleton<IMprisMediaPlayer, MprisMediaPlayer>()
                .AddHostedService<Worker>()
    )
    .Build()
    .RunAsync()
    .ConfigureAwait(false);

string GetConfigFilePath()
{
    // Priority 1: Environment variable for config path
    var configPathEnv = Environment.GetEnvironmentVariable("HASSNOWPLAYING_APPSETTINGS_PATH");
    if (!string.IsNullOrEmpty(configPathEnv) && File.Exists(configPathEnv))
    {
        return configPathEnv;
    }

    // Priority 2: XDG_CONFIG_HOME
    var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
    if (!string.IsNullOrEmpty(xdgConfigHome))
    {
        var xdgConfigPath = Path.Combine(xdgConfigHome, "hass-nowplaying", "appsettings.json");
        if (File.Exists(xdgConfigPath))
        {
            return xdgConfigPath;
        }
    }

    // Priority 3: Check if running as root
    if (Environment.UserName == "root")
    {
        var etcConfigPath = "/etc/hass-nowplaying/appsettings.json";
        if (File.Exists(etcConfigPath))
        {
            return etcConfigPath;
        }
    }

    // Priority 4: Default to user's home directory .config path
    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var userConfigPath = Path.Combine(homeDir, ".config", "hass-nowplaying", "appsettings.json");
    return userConfigPath;
}
