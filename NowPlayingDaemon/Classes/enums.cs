namespace NowPlayingDaemon;

public enum dbusInterface
{
    IMediaPlayer2,
    IPlayer
}

public enum PlaybackStatus
{
    // https://specifications.freedesktop.org/mpris-spec/latest/Player_Interface.html#Enum:Playback_Status
    Playing,
    Paused,
    Stopped
}

public enum RepeatState
{
    off = 0,
    one = 1,
    all = 2
}

public enum LoopStatus
{
    // https://specifications.freedesktop.org/mpris-spec/latest/Player_Interface.html#Enum:Loop_Status
    None = 0,
    Track = 1,
    Playlist = 2
}
