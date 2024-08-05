using Tmds.DBus;

namespace NowPlayingDaemon;

public interface IMprisMediaPlayer
{
    Task SetAsync(dbusInterface iface, string property, object value);
    Task UpdateMetadata(string trackId, long length, string[] artist, string title, string album);

    Task UpdateMetadata(IDictionary<string, object> customMetadata);

    Task SetPlaybackStatus(PlaybackStatus status);

    Task RegisterPlayer(
        Connection connection,
        string identity,
        string desktopEntry,
        bool canControl
    );

    event Action<PropertyChanges> OnPropertiesChanged;
    event Action OnRaise;
    event Action OnQuit;
    event Action OnPlay;
    event Action OnPause;
    event Action OnStop;
    event Action OnPlayPause;
    event Action OnPrevious;
    event Action OnNext;
    event Action OnSeek;
    event Action OnSetPosition;
    event Action OnOpenUri;
}
