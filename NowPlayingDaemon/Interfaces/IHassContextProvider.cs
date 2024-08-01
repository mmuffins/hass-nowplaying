using NetDaemon.HassModel;

namespace NowPlayingDaemon;

public interface IHassContextProvider
{
    IHaContext GetContext();
}
