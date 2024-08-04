using Tmds.DBus;

namespace NowPlayingDaemon;

[DBusInterface(
    "org.mpris.MediaPlayer2.Player",
    GetPropertyMethod = "GetAsync",
    PropertyType = typeof(MprisPlayerProperties),
    SetPropertyMethod = "SetAsync"
)]
public interface IPlayer : IDBusObject
{
    public Task<MprisPlayerProperties> GetAllAsync();
    public Task<object> GetAsync(string property);
    public Task SetAsync(string property, object value);
    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);

    Task PlayAsync();
    Task PauseAsync();
    Task PlayPauseAsync();
    Task StopAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task SeekAsync(long Offset);
    Task SetPositionAsync(ObjectPath TrackId, long Position);
    Task OpenUriAsync(string Uri);
}
