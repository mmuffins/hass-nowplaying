using Tmds.DBus;

namespace NowPlayingDaemon;

[Dictionary]
public class MprisPlayerProperties
{
    public bool CanPlay;
    public bool CanPause;
    public bool CanGoPrevious;
    public bool CanGoNext;
    public bool CanSeek;
    public bool CanControl;
    public PlaybackStatus PlaybackStatus;
    public LoopStatus LoopStatus;
    public double Rate;
    public bool Shuffle;
    public double Volume;
    public long Position;
    public double MinimumRate;
    public double MaximumRate;
    public IDictionary<string, object> Metadata;

    public MprisPlayerProperties()
    {
        CanPlay = false;
        CanPause = false;
        CanGoPrevious = false;
        CanGoNext = false;
        CanSeek = false;
        PlaybackStatus = PlaybackStatus.Stopped;
        LoopStatus = LoopStatus.None;
        Rate = 0;
        Shuffle = false;
        Volume = 0;
        Position = 0;
        MinimumRate = 0;
        MaximumRate = 0;
        CanControl = true;
        Metadata = new Dictionary<string, object>
        {
            { "mpris:trackid", "0" },
            { "mpris:length", 0L }, // Track length in microseconds
            { "xesam:artist", new string[] { "" } },
            { "xesam:title", "" },
            { "xesam:album", "" }
        };
    }
}
