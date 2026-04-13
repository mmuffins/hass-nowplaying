using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using hass_mpris.HassClasses;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using NetDaemon.Runtime;

namespace NowPlayingDaemon;

[NetDaemonApp]
public class HassWorker : BackgroundService, IHassNowPlayingDaemon
{
    private readonly ILogger<HassWorker> _logger;
    private readonly DBusConnectionManager _connectionManager;
    private readonly IMprisMediaPlayer _mprisPlayer;
    private readonly IHassContextProvider _hassContextProvider;
    private readonly UriBuilder hassUrl;
    
    private readonly INetDaemonRuntime _netDaemonRuntime;

    public string MediaPlayerEntityName { get; set; }

    public string PlayerFriendlyName { get; set; } = "Home Assistant";
    public string PlayerDesktopEntry { get; set; } = "hass-nowplaying";
    public int MediaArtSize { get; set; } = 0;

    public HassWorker(
        ILogger<HassWorker> logger,
        IConfiguration config,
        IHassContextProvider hassContextProvider,
        DBusConnectionManager connectionManager,
        IMprisMediaPlayer iMprisMediaPlayer,
        INetDaemonRuntime netDaemonRuntime
    )
    {
        _logger = logger;
        _hassContextProvider = hassContextProvider;
        _connectionManager = connectionManager;
        _mprisPlayer = iMprisMediaPlayer;
        _netDaemonRuntime = netDaemonRuntime;

        int configMediaArtSize;
        if (
            !int.TryParse(config.GetSection("MediaArtSize").Value, out configMediaArtSize)
            || configMediaArtSize < 1
        )
        {
            configMediaArtSize = 0;
        }

        MediaArtSize = configMediaArtSize;

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
        _mprisPlayer.OnStop += Stop;
        _mprisPlayer.OnNext += NextTrack;
        _mprisPlayer.OnPrevious += PreviousTrack;
        _mprisPlayer.OnSeek += Seek;
        _mprisPlayer.OnShuffle += Shuffle;
        _mprisPlayer.OnLoopStatus += LoopStatus;
        _mprisPlayer.OnVolume += Volume;
        _mprisPlayer.OnQuit += TurnOff;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting worker loop.");

        await _netDaemonRuntime.WaitForInitializationAsync();

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
            .StateAllChanges()
            .Where(e => e.New?.Attributes?.MediaContentId != e.Old?.Attributes?.MediaContentId)
            .Subscribe(async s =>
            {
                _logger.LogDebug($"The media content ID of the player has changed.");
                await UpdateMprisMetadata(s.Entity);
            });

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
            .Where(e => e.New?.Attributes?.Shuffle != e.Old?.Attributes?.Shuffle)
            .Subscribe(async s =>
            {
                _logger.LogDebug($"Shuffle on the player was set to {s.New?.Attributes?.Shuffle}.");
                await UpdatePlayerState(s.New);
            });

        haPlayer
            .StateAllChanges()
            .Where(e => e.New?.Attributes?.Repeat != e.Old?.Attributes?.Repeat)
            .Subscribe(async s =>
            {
                _logger.LogDebug($"Repeat on the player was set to {s.New?.Attributes?.Repeat}.");
                await UpdatePlayerState(s.New);
            });

        haPlayer
            .StateAllChanges()
            .Where(e => e.New?.Attributes?.VolumeLevel != e.Old?.Attributes?.VolumeLevel)
            .Subscribe(async s =>
            {
                _logger.LogDebug(
                    $"Volume on the player was set to {s.New?.Attributes?.VolumeLevel}."
                );
                await UpdatePlayerState(s.New);
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

        var shuffle = state.Attributes?.Shuffle ?? false;
        var repeatState = StringtoRepeat(state.Attributes?.Repeat ?? "off");
        var volume = state.Attributes?.VolumeLevel ?? 0.0;
        volume = volume < 0 ? 0 : volume;

        _logger.LogDebug($"Updating mpris player state to {newState}.");

        // some properties can always be propagated regardless of playback status
        await _mprisPlayer.SetShuffle(shuffle);
        await _mprisPlayer.SetLoopStatus((LoopStatus)(int)repeatState);
        await _mprisPlayer.SetVolume(volume);

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
                await _mprisPlayer.SetCanGoNext(false);
                await _mprisPlayer.SetCanGoPrevious(false);
                await _mprisPlayer.UnregisterService();
                return;
        }
    }

    public async Task UpdateMprisMetadata(Entity<MediaPlayerAttributes> haPlayer)
    {
        _logger.LogDebug($"Updating mpris player metadata.");

        var trackId = haPlayer.Attributes?.MediaContentId ?? "";
        var title = haPlayer.Attributes?.MediaTitle ?? "";
        var artist = haPlayer.Attributes?.MediaArtist ?? "";
        var album = haPlayer.Attributes?.MediaAlbumName ?? "";
        var albumArtist = haPlayer.Attributes?.MediaAlbumArtist ?? "";
        var length = haPlayer.Attributes?.MediaDuration ?? 0.0;
        var artUrl = GetMediaArtUrl(haPlayer, MediaArtSize) ?? "";

        // seems like home assistant doesn't publish the media position via its api
        // var position = haPlayer.Attributes?.MediaPosition ?? 0.0;

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

    private string GetMediaArtUrl(Entity<MediaPlayerAttributes> haPlayer, int imageSize)
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

            // The duplicated unescape is intentional, home assistant seems to encode some
            // urls multiple times for some reason
            var decodedImage = Uri.UnescapeDataString(Uri.UnescapeDataString(image));

            // check if the value already is a valid uri
            // if not, it's most likely a relative uri

            Uri? imageUri;
            if (
                Uri.TryCreate(decodedImage, UriKind.Absolute, out imageUri)
                && (imageUri.Scheme == Uri.UriSchemeHttp || imageUri.Scheme == Uri.UriSchemeHttps)
            )
            {
                imageUri = SetArtSizeParameter(imageUri, imageSize);
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
                imageUri = SetArtSizeParameter(imageUri, imageSize);
                return imageUri.ToString();
            }
        }

        return string.Empty;
    }

    private Uri SetArtSizeParameter(Uri uri, int newSize)
    {
        var builder = new UriBuilder(uri);
        var query = System.Web.HttpUtility.ParseQueryString(builder.Query);

        if (query["size"] != null)
        {
            query["size"] = newSize.ToString();
        }

        builder.Query = query.ToString();
        return builder.Uri;
    }

    private Entity<MediaPlayerAttributes>? GetMediaPlayerEntity(IHaContext haContext, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogError("No media player entity ID was provided.");
            return null;
        }

        name = name.Trim();

        _logger.LogDebug("Getting media player {EntityId}.", name);

        if (!name.StartsWith("media_player.", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Configured entity ID '{EntityId}' is invalid. Expected an entity in the media_player domain.",
                name
            );
            return null;
        }

        var exists = haContext.GetAllEntities().Any(e => e.EntityId == name);
        if (!exists)
        {
            _logger.LogError(
                "Configured media player entity '{EntityId}' was not found in the current Home Assistant state cache.",
                name
            );
            return null;
        }

        return new Entity<MediaPlayerAttributes>(haContext, name);
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

        haPlayer.CallService("media_play_pause");
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

        haPlayer.CallService("media_play");
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
        haPlayer.CallService("media_pause");
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
        haPlayer.CallService("media_stop");
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
        haPlayer.CallService("media_next_track");
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

        haPlayer.CallService("media_previous_track");
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

        haPlayer.CallService("media_seek", new { seek_position = offset });
    }

    public void PlayMedia(
        string mediaContentId,
        string mediaContentType,
        string enqueue,
        string announce
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

        haPlayer.CallService(
            "play_media",
            new
            {
                media_content_id = mediaContentId,
                media_content_type = mediaContentType,
                enqueue = enqueue,
                announce = announce
            }
        );
    }


    public void Shuffle(bool enabled)
    {
        _logger.LogDebug($"Setting shuffle to {enabled} on the media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        if (haPlayer.Attributes?.Shuffle == enabled)
        {
            return;
        }

        haPlayer.CallService("shuffle_set", new { shuffle = enabled });
    }

    public void LoopStatus(LoopStatus loopStatus)
    {
        Repeat((RepeatState)(int)loopStatus);
    }

    public void Repeat(RepeatState repeatState)
    {
        _logger.LogDebug($"Setting repeat mode to {repeatState} on the media player.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        if (haPlayer.Attributes?.Repeat?.ToLower() == repeatState.ToString().ToLower())
        {
            return;
        }

        haPlayer.CallService("repeat_set", new { repeat = repeatState.ToString()});
    }

    public void Volume(double volume)
    {
        _logger.LogDebug($"Setting player volume to {volume}.");
        var haContext = _hassContextProvider.GetContext();
        var haPlayer = GetMediaPlayerEntity(haContext, MediaPlayerEntityName);
        if (haPlayer == null)
        {
            _logger.LogError("Could not get media player.");
            return;
        }

        volume = volume < 0 ? 0 : volume;

        if (haPlayer.Attributes?.VolumeLevel == volume)
        {
            return;
        }

        haPlayer.CallService("volume_set", new { volume_level = volume});
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

        haPlayer.CallService("turn_on");
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

        haPlayer.CallService("turn_off");
    }

    private static RepeatState StringtoRepeat(string? state)
    {
        return state?.ToLower() switch
        {
            "one" => RepeatState.one,
            "all" => RepeatState.all,
            _ => RepeatState.off
        };
    }
}
