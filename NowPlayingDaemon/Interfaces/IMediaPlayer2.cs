using Tmds.DBus;

namespace NowPlayingDaemon;

[DBusInterface(
    "org.mpris.MediaPlayer2",
    GetPropertyMethod = "GetAsync",
    PropertyType = typeof(MprisMediaPlayerProperties),
    SetPropertyMethod = "SetAsync"
)]
interface IMediaPlayer2 : IDBusObject
{
    public Task<MprisMediaPlayerProperties> GetAllAsync();
    public Task<object> GetAsync(string property);
    public Task SetAsync(string property, object value);

    Task RaiseAsync();
    Task QuitAsync();
}
