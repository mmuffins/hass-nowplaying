using NetDaemon.HassModel;

namespace NowPlayingDaemon;

public class HassContextProvider : IHassContextProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public HassContextProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IHaContext GetContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IHaContext>();
    }
}
