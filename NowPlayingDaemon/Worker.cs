
using hass_mpris.HassClasses;
using MPRISInterface;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace NowPlayingDaemon;

public interface INowPlaying
{
    // Define your methods here that will be called elsewhere in your application
}


[NetDaemonApp]
public class Worker : BackgroundService, INowPlaying
{
    private readonly ILogger<Worker> _logger;
    private readonly DBusConnectionManager _connectionManager;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, DBusConnectionManager connectionManager)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _connectionManager = connectionManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var haContext = scope.ServiceProvider.GetRequiredService<IHaContext>();
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var haPlayer = new Entity<MediaPlayerAttributes>(haContext, "media_player.sonos_arc");

            var haPlayerX = haContext.GetAllEntities()
                .Where(e => e.EntityId.StartsWith("media_player.sonos_arc"))
                .Select(e => new MediaPlayerEntity(e))
                .First();

            // var atts = (Dictionary<string,object>)entity.Attributes;
            // var pic = atts.Where(a => a.Key == "entity_picture").First();
            // Console.WriteLine(entity.State);
            // Console.WriteLine(entity.Attributes);
            Console.WriteLine(haPlayer.Attributes?.MediaTitle);
            Console.WriteLine(haPlayer.Attributes?.MediaArtist);
            Console.WriteLine(haPlayer.Attributes?.EntityPicture);
            Console.WriteLine(haPlayer.State);

            
        }
    }
}