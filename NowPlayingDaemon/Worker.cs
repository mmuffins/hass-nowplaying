
using hass_mpris.HassClasses;
using MPRISInterface;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

// public interface INowPlaying
// {
//     // Define your methods here that will be called elsewhere in your application
//     Task XPlay();
// }


[NetDaemonApp]
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly DBusConnectionManager _connectionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMprisMediaPlayer _mprisPlayer;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, DBusConnectionManager connectionManager, IMprisMediaPlayer iMprisMediaPlayer)
    {
        // TODO: make it so that mpris media player doesn't need a ton of parameters in its constructor and inject all of that from the worker class
        _logger = logger;
        _scopeFactory = scopeFactory;
        _connectionManager = connectionManager;
        _mprisPlayer = iMprisMediaPlayer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        await _mprisPlayer.RegisterPlayer(_connectionManager.Connection);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2000, stoppingToken);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var haContext = scope.ServiceProvider.GetRequiredService<IHaContext>();
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var haPlayer = new Entity<MediaPlayerAttributes>(haContext, "media_player.sonos_arc");

            var haPlayerX = haContext.GetAllEntities()
                .Where(e => e.EntityId.StartsWith("media_player.sonos_arc"))
                .Select(e => new MediaPlayerEntity(e))
                .First();

            Console.WriteLine(haPlayer.Attributes?.MediaTitle);
            Console.WriteLine(haPlayer.Attributes?.MediaArtist);
            Console.WriteLine(haPlayer.Attributes?.EntityPicture);
            Console.WriteLine(haPlayer.Attributes?.MediaContentId);
            Console.WriteLine(haPlayer.Attributes?.MediaTrack);
            Console.WriteLine(haPlayer.Attributes?.MediaDuration);
            Console.WriteLine(haPlayer.Attributes?.MediaAlbumName);
            
            Console.WriteLine(haPlayer.State);
            await _mprisPlayer.UpdateMetadata(
                haPlayer.Attributes?.MediaContentId, 
                    (long)haPlayer.Attributes?.MediaDuration, 
                    new string[] { haPlayer.Attributes?.MediaArtist } , 
                    haPlayer.Attributes?.MediaTitle, 
                    haPlayer.Attributes?.MediaAlbumName
            );
        }
    }
}