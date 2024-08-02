using hass_mpris.HassClasses;
using NetDaemon.HassModel;

namespace NowPlayingDaemon;

public interface IHassNowPlayingDaemon
{
    void PlayPause();
    public Task UpdateMprisMetadata();
}
