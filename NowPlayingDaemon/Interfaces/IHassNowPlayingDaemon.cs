using hass_mpris.HassClasses;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

public interface IHassNowPlayingDaemon
{
    void PlayPause();
    public Task UpdateMprisMetadata();
    public Task UpdatePlayerState(
        StateChange<MediaPlayerEntity, EntityState<MediaPlayerAttributes>> state
    );
}
