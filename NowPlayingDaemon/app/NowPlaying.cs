// global using System;
// using System.Collections.Generic;
// using NetDaemon.Client.HomeAssistant.Model;
using NetDaemon.HassModel.Entities;
using hass_mpris.HassClasses;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NetDaemon.HassModel;
using NetDaemon.AppModel;

namespace NowPlayingDaemon;


[NetDaemonApp]
public class NowPlaying
{
    private readonly ILogger<NowPlaying> logger;

    public NowPlaying(IHaContext haContext, ILogger<NowPlaying> logger)
    {
        this.logger = logger;
        logger.LogInformation("This is a log message from MyService.");

        // ha.CallService("notify", "persistent_notification", data: new {message = "Notify meXX", title = "Hello world!"});

        // logger.LogInformation("test");
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
        
        // haPlayerX.MediaPlayPause();
    }
}