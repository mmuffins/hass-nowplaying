using hass_mpris.HassClasses;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

public interface IHassNowPlayingDaemon
{
    void PlayPause();
    void Play();
    void Pause();
    void Stop();
    void NextTrack();
    void PreviousTrack();

    void TurnOn();
    void TurnOff();

    public Task UpdateMprisMetadata(MediaPlayerEntity haPlayer);
    public Task UpdatePlayerState(EntityState<MediaPlayerAttributes>? state);
}
