using Tmds.DBus;

namespace NowPlayingDaemon;

public interface IMprisMediaPlayer
{
    Task UpdateMetadata(string trackId, long length, string[] artist, string title, string album);

    Task UpdateMetadata(IDictionary<string, object> customMetadata);

    Task RegisterPlayer(
        Connection connection,
        string identity,
        string desktopEntry,
        bool canControl
    );

    public event Action OnPlayPause;
}
