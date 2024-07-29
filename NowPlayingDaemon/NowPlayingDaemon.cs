// global using System;
// global using System.Reactive.Linq;
// global using Microsoft.Extensions.Logging;
// global using NetDaemon.AppModel;
// global using NetDaemon.HassModel;
// using System.Collections.Generic;
// using System.Linq;
// using NetDaemon.Client.HomeAssistant.Model;
using NetDaemon.HassModel.Entities;
using hass_mpris.HassClasses;
using System.Linq;

namespace NowPlayingDaemon;

/// <summary>
///     Hello world showcase using the new HassModel API
/// </summary>
[NetDaemonApp]
public class NowPlaying
{
    public NowPlaying(IHaContext haContext)
    {
        // ha.CallService("notify", "persistent_notification", data: new {message = "Notify meXX", title = "Hello world!"});

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
        haPlayerX.MediaPlayPause();
    }
}