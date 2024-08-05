using Tmds.DBus;

namespace NowPlayingDaemon;

public interface IMprisMediaPlayer
{
    public ObjectPath ObjectPath { get; }
    public string ServiceName { get; }

    Task SetAsync(dbusInterface iface, string property, object value);
    Task UpdateMetadata(string trackId, long length, string[] artist, string title, string album);

    Task UpdateMetadata(IDictionary<string, object> customMetadata);

    Task SetPlaybackStatus(PlaybackStatus status);
    Task SetCanPlay(bool state);
    Task SetCanPause(bool state);
    Task SetCanQuit(bool state);
    Task SetCanGoPrevious(bool state);
    Task SetCanGoNext(bool state);
    Task RegisterPlayer(string identity, string desktopEntry);
    void UnregisterPlayer();
    Task RegisterService();
    Task UnregisterService();

    Task RaiseAsync();
    Task QuitAsync();
    Task PlayAsync();
    Task PauseAsync();
    Task PlayPauseAsync();
    Task StopAsync();
    Task PreviousAsync();
    Task NextAsync();
    Task SeekAsync(long offset);
    Task SetPositionAsync(ObjectPath TrackId, long Position);
    Task OpenUriAsync(string Uri);

    event Action<PropertyChanges> OnPropertiesChanged;
    event Action OnRaise;
    event Action OnQuit;
    event Action OnPlay;
    event Action OnPause;
    event Action OnStop;
    event Action OnPlayPause;
    event Action OnPrevious;
    event Action OnNext;
    event Action<long> OnSeek;
    event Action<string, long> OnSetPosition;
    event Action<string> OnOpenUri;
}
