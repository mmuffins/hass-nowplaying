using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using hass_mpris.HassClasses;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

[NetDaemonApp]
public class Worker : BackgroundService, IHassNowPlayingDaemon
{
    private readonly ILogger<Worker> _logger;
    private readonly DBusConnectionManager _connectionManager;
    private readonly IMprisMediaPlayer _mprisPlayer;
    private readonly IHassContextProvider _hassContextProvider;

    private readonly UriBuilder hassUrl;

    public string MediaPlayerEntityName { get; set; }

    public string PlayerFriendlyName { get; set; } = "Home Assistant";
    public string PlayerDesktopEntry { get; set; } = "hass-nowplaying";

    public Worker(
        ILogger<Worker> logger,
        IConfiguration config,
        IHassContextProvider hassContextProvider,
        DBusConnectionManager connectionManager,
        IMprisMediaPlayer iMprisMediaPlayer
    )
    {
        _logger = logger;
        _hassContextProvider = hassContextProvider;
        _connectionManager = connectionManager;
        _mprisPlayer = iMprisMediaPlayer;

        hassUrl = new UriBuilder
        {
            Scheme = config.GetValue<bool>("HomeAssistant:Ssl") ? "https" : "http",
            Host = config.GetValue<string>("HomeAssistant:Host"),
            Port = config.GetValue<int>("HomeAssistant:Port")
        };

        var mediaPlayerEntity = config.GetValue<string>("MediaplayerEntity");

        if (string.IsNullOrEmpty(mediaPlayerEntity))
        {
            throw new InvalidOperationException("MediaplayerEntity setting must be configured.");
        }

        MediaPlayerEntityName = mediaPlayerEntity;

        _mprisPlayer.OnPlayPause += PlayPause;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting worker loop.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);

        var friendlyName = haPlayer.Attributes?.FriendlyName;
        if (!string.IsNullOrEmpty(friendlyName))
        {
            PlayerFriendlyName = friendlyName;
        }

        haPlayer
            .StateAllChanges()
            .Where(e => e.New?.Attributes?.MediaContentId != e.Old?.Attributes?.MediaContentId)
            .Subscribe(async s =>
            {
                _logger.LogDebug($"The media content ID of the player has changed.");
                await UpdateMprisMetadata();
            });

        await _mprisPlayer.RegisterPlayer(
            _connectionManager.Connection,
            PlayerFriendlyName,
            PlayerDesktopEntry,
            false
        );

        await UpdateMprisMetadata();
    }

    public async Task UpdateMprisMetadata()
    {
        _logger.LogDebug($"Updating mpris player metadata.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);

        var trackId = haPlayer.Attributes?.MediaContentId ?? "";
        var url = haPlayer.Attributes?.MediaContentId ?? "";
        var title = haPlayer.Attributes?.MediaTitle ?? "";
        var artist = haPlayer.Attributes?.MediaArtist ?? "";
        var album = haPlayer.Attributes?.MediaAlbumName ?? "";
        var albumArtist = haPlayer.Attributes?.MediaAlbumArtist ?? "";
        var length = haPlayer.Attributes?.MediaDuration ?? 0.0;
        var position = haPlayer.Attributes?.MediaPosition ?? 0.0;
        var artUrl = GetMediaUrl(haPlayer) ?? "";

        // var shuffle = haPlayer.Attributes?.Shuffle ?? false;
        // var repeat = haPlayer.Attributes?.Repeat ?? "";
        // var volume = haPlayer.Attributes?.VolumeLevel ?? 0.0;
        // var state = haPlayer.State;

        var metadata = new Dictionary<string, object>();
        metadata.Add("mpris:trackid", trackId);
        metadata.Add("mpris:length", length);
        metadata.Add("mpris:artUrl", artUrl);
        metadata.Add("xesam:album", album);
        metadata.Add("xesam:artist", new string[] { artist });
        metadata.Add("xesam:albumArtist", new string[] { albumArtist });
        metadata.Add("xesam:title", title);
        metadata.Add("xesam:url", url);

        Console.WriteLine(haPlayer.Attributes?.MediaTrack);

        Console.WriteLine(haPlayer.State);

        _logger.LogInformation($"Now Playing: {artist}: {title}.");

        await _mprisPlayer.UpdateMetadata(metadata);
    }

    private string GetMediaUrl(MediaPlayerEntity haPlayer)
    {
        // how the image uri is provided can very greatly between players
        // so we need to check each possible values
        string[] images =
        [
            haPlayer.Attributes?.MediaImageUrl ?? "",
            haPlayer.Attributes?.EntityPicture ?? "",
            haPlayer.Attributes?.EntityPictureLocal ?? "",
        ];

        foreach (var image in images)
        {
            if (string.IsNullOrEmpty(image))
            {
                continue;
            }

            // check if the value already is a valid uri
            // if not, it's most likely a relative uri
            var decodedImage = Uri.UnescapeDataString(image);
            Uri imageUri;
            if (
                Uri.TryCreate(decodedImage, UriKind.Absolute, out imageUri)
                && (imageUri.Scheme == Uri.UriSchemeHttp || imageUri.Scheme == Uri.UriSchemeHttps)
            )
            {
                return imageUri.ToString();
            }

            if (
                Uri.TryCreate(
                    $"{hassUrl.Uri.AbsoluteUri.ToString().TrimEnd('/')}/{decodedImage.TrimStart('/')}",
                    UriKind.Absolute,
                    out imageUri
                ) && (imageUri.Scheme == Uri.UriSchemeHttp || imageUri.Scheme == Uri.UriSchemeHttps)
            )
            {
                return imageUri.ToString();
            }
        }

        return null;
    }

    private MediaPlayerEntity GetMediaPlayerEntity(IHaContext haContext, string name)
    {
        _logger.LogDebug($"Getting media player {name}.");
        // var haPlayerX = new Entity<MediaPlayerAttributes>(haContext, MediaPlayerEntityName);
        return haContext
            .GetAllEntities()
            .Where(e => e.EntityId.StartsWith(name))
            .Select(e => new MediaPlayerEntity(e))
            .First();
    }

    public void PlayPause()
    {
        _logger.LogDebug("Sending PlayPause signal to player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);

        haPlayer.MediaPlayPause();
    }
}
