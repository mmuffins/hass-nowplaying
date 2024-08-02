using System.Reflection;
using Microsoft.Extensions.Hosting;
using NetDaemon.AppModel;
using NetDaemon.Extensions.Logging;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.Extensions.Tts;
using NetDaemon.Runtime;
using NowPlayingDaemon;
// Add next line if using code generator
//using HomeAssistantGenerated;

#pragma warning disable CA1812


try
{
    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var configDir = System.IO.Path.Combine(homeDir, ".config", "hass-nowplaying");


    await Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(configDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        // .UseNetDaemonAppSettings()
        // .UseNetDaemonDefaultLogging()
        .UseNetDaemonRuntime()
        .UseSystemd()
        // .UseNetDaemonTextToSpeech()
        .ConfigureServices((_, services) =>
            services
                // .AddNetDaemonStateManager()
                // .AddNetDaemonScheduler()
                // .AddAppsFromAssembly(Assembly.GetExecutingAssembly())
                .AddSingleton<DBusConnectionManager>()
                .AddSingleton<IHassContextProvider, HassContextProvider>()
                .AddSingleton<IMprisMediaPlayer, MprisMediaPlayer>()
                .AddHostedService<Worker>()
                
                // Add next line if using code generator
                // .AddHomeAssistantGenerated()
        )
        .Build()
        .RunAsync()
        .ConfigureAwait(false);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to start host... {e}");
    throw;
}