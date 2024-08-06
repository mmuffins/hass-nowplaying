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
    void Shuffle(bool enabled);
    void LoopStatus(LoopStatus loopStatus);
    void Repeat(RepeatState repeatState);
    void Volume(double volume);
    void Seek(long offset);
    void PlayMedia(string mediaContentId, string mediaContentType, object enqueue, bool announce);

    void TurnOn();
    void TurnOff();

    public Task UpdateMprisMetadata(MediaPlayerEntity haPlayer);
    public Task UpdatePlayerState(EntityState<MediaPlayerAttributes>? state);
}
