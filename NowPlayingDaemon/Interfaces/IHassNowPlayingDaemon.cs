using hass_mpris.HassClasses;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

public interface IHassNowPlayingDaemon
{
    void PlayPause();
    public Task UpdateMprisMetadata(MediaPlayerEntity haPlayer);
    public Task UpdatePlayerState(EntityState<MediaPlayerAttributes>? state);
}
