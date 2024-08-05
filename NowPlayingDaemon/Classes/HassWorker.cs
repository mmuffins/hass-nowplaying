using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using hass_mpris.HassClasses;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

[NetDaemonApp]
public class HassWorker : BackgroundService, IHassNowPlayingDaemon
{
    private readonly ILogger<HassWorker> _logger;
    private readonly DBusConnectionManager _connectionManager;
    private readonly IMprisMediaPlayer _mprisPlayer;
    private readonly IHassContextProvider _hassContextProvider;

    private readonly UriBuilder hassUrl;

    public string MediaPlayerEntityName { get; set; }

    public string PlayerFriendlyName { get; set; } = "Home Assistant";
    public string PlayerDesktopEntry { get; set; } = "hass-nowplaying";

    public HassWorker(
        ILogger<HassWorker> logger,
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
        _mprisPlayer.OnPlay += Play;
        _mprisPlayer.OnPause += Pause;
        _mprisPlayer.OnNext += NextTrack;
        _mprisPlayer.OnPrevious += PreviousTrack;
        _mprisPlayer.OnSeek += Seek;
        _mprisPlayer.OnStop += Stop;
        _mprisPlayer.OnQuit += TurnOff;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting worker loop.");

        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        var friendlyName = haPlayer.Attributes?.FriendlyName;
        if (!string.IsNullOrEmpty(friendlyName))
        {
            PlayerFriendlyName = friendlyName;
        }

        _logger.LogDebug("Subscribing to player events.");
        haPlayer
            .StateChanges()
            .Subscribe(async s =>
            {
                _logger.LogDebug(
                    $"The state of the player has changed from {s.Old?.State} to {s.New?.State}."
                );
                await UpdatePlayerState(s.New);
            });

        haPlayer
            .StateAllChanges()
            .Where(e => e.New?.Attributes?.MediaContentId != e.Old?.Attributes?.MediaContentId)
            .Subscribe(async s =>
            {
                _logger.LogDebug($"The media content ID of the player has changed.");
                await UpdateMprisMetadata(s.Entity);
            });

        await _mprisPlayer.RegisterPlayer(PlayerFriendlyName, PlayerDesktopEntry);

        await UpdatePlayerState(haPlayer.EntityState);
        await UpdateMprisMetadata(haPlayer);

        await _mprisPlayer.RegisterService();
    }

    public async Task UpdatePlayerState(EntityState<MediaPlayerAttributes>? state)
    {
        if (state == null)
        {
            return;
        }

        var newState = state.State;

        _logger.LogDebug($"Updating mpris player state to {newState}.");

        switch (newState)
        {
            case "playing":
                await _mprisPlayer.SetPlaybackStatus(PlaybackStatus.Playing);
                await _mprisPlayer.SetCanPlay(true);
                await _mprisPlayer.SetCanPause(true);
                await _mprisPlayer.SetCanQuit(true);
                await _mprisPlayer.SetCanGoNext(true);
                await _mprisPlayer.SetCanGoPrevious(true);
                await _mprisPlayer.RegisterService();
                return;

            case "paused":
                await _mprisPlayer.SetPlaybackStatus(PlaybackStatus.Paused);
                await _mprisPlayer.SetCanPlay(true);
                await _mprisPlayer.SetCanPause(true);
                await _mprisPlayer.SetCanQuit(true);
                await _mprisPlayer.SetCanGoNext(true);
                await _mprisPlayer.SetCanGoPrevious(true);
                await _mprisPlayer.RegisterService();
                return;

            case "idle":
                await _mprisPlayer.SetPlaybackStatus(PlaybackStatus.Stopped);
                await _mprisPlayer.SetCanPlay(true);
                await _mprisPlayer.SetCanPause(true);
                await _mprisPlayer.SetCanQuit(true);
                await _mprisPlayer.SetCanGoNext(true);
                await _mprisPlayer.SetCanGoPrevious(true);
                await _mprisPlayer.RegisterService();
                return;

            case "off":
                await _mprisPlayer.SetPlaybackStatus(PlaybackStatus.Stopped);
                await _mprisPlayer.SetCanPlay(false);
                await _mprisPlayer.SetCanPause(false);
                await _mprisPlayer.SetCanQuit(false);
                await _mprisPlayer.SetCanGoNext(false);
                await _mprisPlayer.SetCanGoPrevious(false);
                await _mprisPlayer.UnregisterService();
                return;

            default:
                _logger.LogError($"Unknown player state '{newState}'");
                await _mprisPlayer.SetCanPlay(false);
                await _mprisPlayer.SetCanPause(false);
                await _mprisPlayer.SetCanQuit(false);
                await _mprisPlayer.UnregisterService();

                return;
        }
    }

    public async Task UpdateMprisMetadata(MediaPlayerEntity haPlayer)
    {
        _logger.LogDebug($"Updating mpris player metadata.");

        var trackId = haPlayer.Attributes?.MediaContentId ?? "";
        var title = haPlayer.Attributes?.MediaTitle ?? "";
        var artist = haPlayer.Attributes?.MediaArtist ?? "";
        var album = haPlayer.Attributes?.MediaAlbumName ?? "";
        var albumArtist = haPlayer.Attributes?.MediaAlbumArtist ?? "";
        var length = haPlayer.Attributes?.MediaDuration ?? 0.0;
        // var position = haPlayer.Attributes?.MediaPosition ?? 0.0;
        var artUrl = GetMediaUrl(haPlayer) ?? "";

        // var shuffle = haPlayer.Attributes?.Shuffle ?? false;
        // var repeat = haPlayer.Attributes?.Repeat ?? "";
        // var volume = haPlayer.Attributes?.VolumeLevel ?? 0.0;

        var metadata = new Dictionary<string, object>();
        metadata.Add("mpris:trackid", trackId);
        metadata.Add("mpris:length", length);
        metadata.Add("mpris:artUrl", artUrl);
        metadata.Add("xesam:album", album);
        metadata.Add("xesam:artist", new string[] { artist });
        metadata.Add("xesam:albumArtist", new string[] { albumArtist });
        metadata.Add("xesam:title", title);

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
            Uri? imageUri;
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

        return string.Empty;
    }

    private MediaPlayerEntity? GetMediaPlayerEntity(IHaContext haContext, string name)
    {
        _logger.LogDebug($"Getting media player {name}.");
        // var haPlayerX = new Entity<MediaPlayerAttributes>(haContext, MediaPlayerEntityName);

        IEnumerable<MediaPlayerEntity> foundEntities = haContext
            .GetAllEntities()
            .Where(e => e.EntityId == name)
            .Select(e => new MediaPlayerEntity(e));

        if (foundEntities.Count() == 0)
        {
            _logger.LogError($"Could not find entity with name '{name}'.");
            return null;
        }

        if (foundEntities.Count() > 1)
        {
            string allNames = string.Join(", ", foundEntities.Select(p => p.EntityId));
            _logger.LogError(
                $"Found multiple entities matching provided name '{name}': {allNames}"
            );
            return null;
        }

        return foundEntities.First();
    }

    public void PlayPause()
    {
        _logger.LogDebug("Sending playpause signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        haPlayer.MediaPlayPause();
    }

    public void Play()
    {
        _logger.LogDebug("Sending play signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        if (haPlayer.State == "playing")
        {
            return;
        }

        haPlayer.MediaPlay();
    }

    public void Pause()
    {
        _logger.LogDebug("Sending pause signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        if (haPlayer.State == "paused")
        {
            return;
        }
        haPlayer.MediaPause();
    }

    public void Stop()
    {
        _logger.LogDebug("Sending stop signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        if (haPlayer.State == "idle")
        {
            return;
        }
        haPlayer.MediaStop();
    }

    public void NextTrack()
    {
        _logger.LogDebug("Sending next track signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        // According to https://specifications.freedesktop.org/mpris-spec/latest/Player_Interface.html
        // the next and previous actions should fire even if it's not know if they will be successful
        // so we don't really need to check for anything
        haPlayer.MediaNextTrack();
    }

    public void PreviousTrack()
    {
        _logger.LogDebug("Sending previous track signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        haPlayer.MediaPreviousTrack();
    }

    public void Seek(long offset)
    {
        _logger.LogDebug($"Sending seek signal to with offset {offset} media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        haPlayer.MediaSeek(offset);
    }

    public void PlayMedia(
        string mediaContentId,
        string mediaContentType,
        object enqueue,
        bool announce
    )
    {
        _logger.LogDebug($"Sending play media signal for id {mediaContentId} media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        haPlayer.PlayMedia(mediaContentId, mediaContentType, enqueue, announce);
    }

    public void TurnOn()
    {
        _logger.LogDebug("Sending on signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        if (haPlayer.IsOn())
        {
            return;
        }

        haPlayer.TurnOn();
    }

    public void TurnOff()
    {
        _logger.LogDebug("Sending off signal to media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        if (haPlayer.IsOn())
        {
            return;
        }

        haPlayer.TurnOff();
    }
}
