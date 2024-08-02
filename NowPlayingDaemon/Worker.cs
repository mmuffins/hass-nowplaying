using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using hass_mpris.HassClasses;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

public interface IHassNowPlayingDaemon
{
    // Define your methods here that will be called elsewhere in your application
    void PlayPause();
}

[NetDaemonApp]
public class Worker : BackgroundService, IHassNowPlayingDaemon
{
    private readonly ILogger<Worker> _logger;
    private readonly DBusConnectionManager _connectionManager;
    private readonly IMprisMediaPlayer _mprisPlayer;
    private readonly IHassContextProvider _hassContextProvider;

    public string MediaPlayerEntityName { get; set; }

    public Worker(
        ILogger<Worker> logger,
        IHassContextProvider hassContextProvider,
        DBusConnectionManager connectionManager,
        IMprisMediaPlayer iMprisMediaPlayer
    )
    {
        _logger = logger;
        _hassContextProvider = hassContextProvider;
        _connectionManager = connectionManager;
        _mprisPlayer = iMprisMediaPlayer;

        MediaPlayerEntityName = "media_player.sonos_arc";

        _mprisPlayer.OnPlayPause += PlayPause;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting worker loop.");
        await _mprisPlayer.RegisterPlayer(
            _connectionManager.Connection,
            "testPlayer",
            "testplayer",
            false
        );
        var haContext = _hassContextProvider.GetContext();

        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);

        haPlayer
            .StateAllChanges()
            .Where(e => e.New?.Attributes?.MediaContentId != e.Old?.Attributes?.MediaContentId)
            .Subscribe(async s =>
            {
                _logger.LogDebug($"The media content ID of the player has changed.");
                await UpdateMprisMetadata();
            });

        // while (!stoppingToken.IsCancellationRequested)
        // {
        //     await Task.Delay(5000, stoppingToken);
        // }
    }

    private async Task UpdateMprisMetadata()
    {
        _logger.LogDebug($"Updating mpris player metadata.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
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
            new string[] { haPlayer.Attributes?.MediaArtist },
            haPlayer.Attributes?.MediaTitle,
            haPlayer.Attributes?.MediaAlbumName
        );
    }

    private MediaPlayerEntity GetMediaPlayerEntity(IHaContext haContext, string name)
    {
        _logger.LogDebug($"Getting media player with name {name}.");
        // var haPlayerX = new Entity<MediaPlayerAttributes>(haContext, "media_player.sonos_arc");
        return haContext
            .GetAllEntities()
            .Where(e => e.EntityId.StartsWith(name))
            .Select(e => new MediaPlayerEntity(e))
            .First();
    }

    public async void PlayPause()
    {
        _logger.LogDebug("Sending PlayPause signal to home assistant.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);

        haPlayer.MediaPlayPause();
    }
}
